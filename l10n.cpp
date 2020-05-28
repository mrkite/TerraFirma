/** @copyright 2020 seancode */

/*
 * This crazy chunk of code parses a .Net executable and extracts the
 * managed resources from it.  We need this to get the .json translation
 * files from terraria.
 */

#include <QSharedPointer>
#include <QRegularExpression>
#include <QJsonDocument>
#include "l10n.h"
#include "handle.h"

enum {
  TILDE = 0,
  STRINGS = 1,
};

struct Stream {
  quint32 offset, size;
};

struct Resource {
  Resource(quint32 name, quint32 offset) : name(name), offset(offset) {}
  quint32 name, offset;
};

static quint32 fits0(quint32 v) {
  return v > 65535 ? 4 : 2;
}
static quint32 fits1(quint32 a, quint32 b) {
  return qMax(a, b) > 32767 ? 4 : 2;
}
static quint32 fits2(quint32 a, quint32 b, quint32 c, quint32 d = 0) {
  return qMax(qMax(qMax(a, b), c), d) > 16383 ? 4 : 2;
}
static quint32 fits3(quint32 a, quint32 b, quint32 c = 0, quint32 d = 0,
                     quint32 e = 0) {
  return qMax(qMax(qMax(qMax(a, b), c), d), e) > 8192 ? 4 : 2;
}
static quint32 fits5(quint32 a) {
  return a > 2047 ? 4 : 2;
}

L10n::L10n() {

}

void L10n::load(QString exe) {
  auto handle = QSharedPointer<Handle>(new Handle(exe));
  if (!handle->exists()) {
    return;
  }
  if (handle->r16() != 0x5a4d) {  // not an MZ exe
    return;
  }
  handle->seek(0x3c);
  handle->seek(handle->r32());
  if (handle->r32() != 0x4550) {  // not a PE exe
    return;
  }
  handle->skip(2);
  auto numSections = handle->r16();
  handle->skip(12);
  auto headerLen = handle->r16();
  handle->skip(headerLen + 2);
  bool found = false;
  quint32 base, offset;
  for (quint16 i = 0; i < numSections; i++) {
    if (handle->read(5) == ".text") {
      handle->skip(7);
      base = handle->r32();
      handle->skip(4);
      offset = handle->r32();
      found = true;
      break;
    }
    handle->skip(35);
  }
  if (!found) {  // doesn't contain a .text segment
    return;
  }
  handle->seek(offset + 0x10);
  quint32 metaRVA = handle->r32();
  handle->skip(12);
  quint32 resourceRVA = handle->r32();

  handle->seek(metaRVA + offset - base + 0xc);
  quint32 verLen = handle->r32();
  handle->skip(verLen + 2);
  Stream streams[2];
  quint16 numStreams = handle->r16();
  for (quint16 i = 0; i < numStreams; i++) {
    quint32 strOfs = handle->r32();
    quint32 strSize = handle->r32();
    int type = -1;
    QString tname;
    quint8 ch;
    do {
      ch = handle->r8();
      if (ch) {
        tname += QChar(ch);
      }
    } while (ch);
    while (handle->tell() & 3) {
      handle->skip(1);
    }
    if (tname == "#~") {
      type = TILDE;
    } else if (tname == "#Strings") {
      type = STRINGS;
    }
    if (type >= 0) {
      streams[type].offset = strOfs;
      streams[type].size = strSize;
    }
  }
  handle->seek(metaRVA + streams[TILDE].offset + offset - base + 6);
  auto indexWidths = handle->r16();
  int strWidth = (indexWidths & 1) ? 4 : 2;
  int guidWidth = (indexWidths & 2) ? 4 : 2;
  int blobWidth = (indexWidths & 4) ? 4 : 2;
  auto tables = handle->r64();
  handle->skip(8);
  quint32 rows[64];
  for (int i = 0; i < 64; i++) {
    if (tables & 1) {
      rows[i] = handle->r32();
    } else {
      rows[i] = 0;
    }
    tables >>= 1;
  }

  const int TypeDefOrRef = fits2(rows[1], rows[2], rows[27]);
  const int MethodDefOrRef = fits1(rows[6], rows[10]);
  quint32 CustomAttr = rows[0];
  int customRows[] = {
    1, 2, 4, 6, 8, 9, 10, 17, 20, 23, 26, 27, 32, 35, 38, 39, 40
  };
  for (quint16 i = 0; i < sizeof(customRows) / sizeof(customRows[0]); i++) {
    if (rows[customRows[i]] > CustomAttr) {
      CustomAttr = rows[customRows[i]];
    }
  }
  /*
   * Ugh, in order to seek into the stream, you need to handle all the types
   * in order.  Since we want resources, which is type 40, we need to
   * calculate the sizes of types 0 - 39. */
  quint32 skip =
      // Module
      rows[0] * (2 + strWidth + guidWidth * 3) +
      // TypeRef
      rows[1] * (fits2(rows[0], rows[1], rows[26], rows[35]) + strWidth * 2) +
      // TypeDef
      rows[2] * (4 + strWidth * 2 + TypeDefOrRef + fits0(rows[4]) + fits0(rows[6])) +
      // Field
      rows[4] * (2 + strWidth + blobWidth) +
      // MehtodDef
      rows[6] * (8 + strWidth + blobWidth + fits0(rows[8])) +
      // Param
      rows[8] * (4 + strWidth) +
      // InterfaceImpl
      rows[9] * (fits0(rows[2]) + TypeDefOrRef) +
      // MemberRef
      rows[10] * (fits3(rows[1], rows[2], rows[6], rows[26],rows[27]) +
      strWidth + blobWidth) +
      // Constant
      rows[11] * (2 + fits2(rows[4], rows[8], rows[23]) + blobWidth) +
      // CustomAttribute
      rows[12] * (fits5(CustomAttr) + fits3(rows[6], rows[10]) + blobWidth) +
      // FieldMarshal
      rows[13] * (fits1(rows[4], rows[8]) + blobWidth) +
      // DeclSecurity
      rows[14] * (2 + fits2(rows[2], rows[6], rows[32]) + blobWidth) +
      // ClassLayout
      rows[15] * (6 + fits0(rows[2])) +
      // FieldLayout
      rows[16] * (4 + fits0(rows[4])) +
      // StandAloneSig
      rows[17] * blobWidth +
      // EventMap
      rows[18] * (fits0(rows[2]) + fits0(rows[20])) +
      // Event
      rows[20] * (2 + strWidth + TypeDefOrRef) +
      // PropertyMap
      rows[21] * (fits0(rows[2]) + fits0(rows[23])) +
      // Property
      rows[23] * (2 + strWidth + blobWidth) +
      // MethodSemantics
      rows[24] * (2 + fits0(rows[6]) + fits1(rows[20], rows[23])) +
      // MethodImpl
      rows[25] * (fits0(rows[2]) + MethodDefOrRef * 2) +
      // ModuleRef
      rows[26] * strWidth +
      // TypeSpec
      rows[27] * blobWidth +
      // ImplMap
      rows[28] * (2 + fits1(rows[4], rows[6]) + strWidth + fits0(rows[26])) +
      // FieldRVA
      rows[29] * (4 + fits0(rows[4])) +
      // Assembly
      rows[32] * (16 + blobWidth + strWidth * 2) +
      // AssemblyProcessor
      rows[33] * 4 +
      // AssemblyOS
      rows[34] * 12 +
      // AssemblyRef
      rows[35] * (12 + blobWidth * 2 + strWidth * 2) +
      // AssemblyRefProcessor
      rows[36] * (4 + fits0(rows[35])) +
      // AssemblyRefOS
      rows[37] * (12 + fits0(rows[35])) +
      // File
      rows[38] * (4 + strWidth + blobWidth) +
      // ExportedType
      rows[39] * (8 + strWidth * 2 + fits2(rows[35], rows[38], rows[39]));
  handle->skip(skip);
  quint32 resLen = fits2(rows[35], rows[38], rows[39]);
  QList<QSharedPointer<Resource>> resources;
  for (quint32 i = 0; i < rows[40]; i++) {
    quint32 ofs = handle->r32();
    handle->skip(4);
    quint32 name = strWidth == 4 ? handle->r32() : handle->r16();
    handle->skip(resLen);
    resources.append(QSharedPointer<Resource>(new Resource(name, ofs)));
  }
  handle->seek(metaRVA + streams[STRINGS].offset + offset - base);
  QRegularExpression re("Terraria\\.Localization\\.Content\\.([^.]+)\\.([^.]+)\\.json");
  for (const auto &r : resources) {
    handle->seek(metaRVA + streams[STRINGS].offset + offset - base + r->name);
    auto name = handle->rcs();
    auto match = re.match(name);
    if (match.hasMatch()) {
      auto lang = match.captured(1);

      handle->seek(r->offset + resourceRVA + offset - base);
      auto len = handle->r32();

      QString raw = QString::fromUtf8(handle->readBytes(len), len);
      QRegularExpression comma(",\\s*}");
      raw.replace(comma, "}");  // remove trailing commas
      QJsonParseError error;
      QJsonDocument doc = QJsonDocument::fromJson(raw.toUtf8(), &error);
      if (!doc.isNull() && doc.isObject()) {
        auto root = doc.object();
        languages.insert(lang);
        if (match.captured(2) == "Items") {
          items[lang] = root.value("ItemName").toObject();
        } else if (match.captured(2) == "NPCs") {
          npcs[lang] = root.value("NPCName").toObject();
        }
      }
    }
  }
}

void L10n::setLanguage(QString lang) {
  currentLanguage = lang;
}

QList<QString> L10n::getLanguages() {
  return languages.toList();
}

QString L10n::xlateItem(const QString &key) {
  auto str = items[currentLanguage].value(key).toString(key);
  QRegularExpression re("{\\$ItemName\\.(.+?)}");
  auto match = re.match(str);
  if (match.hasMatch()) {
    str.replace(re, xlateItem(match.captured(1)));
  }
  return str;
}

QString L10n::xlateNPC(const QString &key) {
  auto str = npcs[currentLanguage].value(key).toString(key);
  QRegularExpression re("{\\$NPCName\\.(.+?)}");
  auto match = re.match(str);
  if (match.hasMatch()) {
    str.replace(re, xlateNPC(match.captured(1)));
  }
  return str;
}

