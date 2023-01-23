/** @copyright 2023 Sean Kasun */
#include <QColorDialog>
#include "tileeditor.h"
#include "ui_tileeditor.h"

TileEditor::TileEditor(QJsonObject obj, QWidget *parent) : QDialog(parent), ui(new Ui::TileEditor), obj(obj) {
  ui->setupUi(this);
  ui->id->setText(tr("%1").arg(obj["id"].toInt()));
  if (obj.contains("ref")) {
    ui->name->setText(tr("%1").arg(obj["ref"].toInt()));
  } else {
    ui->name->setText(obj["name"].toString());
  }
  ui->color->setText(obj["color"].toString());
  auto flags = obj["flags"].toInt();
  ui->solid->setChecked(flags & 1);
  ui->trans->setChecked(flags & 2);
  ui->dirt->setChecked(flags & 4);
  ui->stone->setChecked(flags & 8);
  ui->grass->setChecked(flags & 0x10);
  ui->pile->setChecked(flags & 0x20);
  ui->flip->setChecked(flags & 0x40);
  ui->brick->setChecked(flags & 0x80);
  ui->moss->setChecked(flags & 0x100);
  ui->mergef->setChecked(flags & 0x200);
  ui->large->setChecked(flags & 0x400);
  ui->merge->setText(obj["merge"].toString());
  ui->blend->setText(obj["blend"].toString());
  ui->skipy->setValue(obj["skipy"].toInt());
  ui->toppad->setValue(obj["toppad"].toInt());
  ui->w->setValue(obj["w"].toInt(18));
  ui->h->setValue(obj["h"].toInt(18));
  ui->red->setValue(obj["r"].toDouble());
  ui->green->setValue(obj["g"].toDouble());
  ui->blue->setValue(obj["b"].toDouble());
}

TileEditor::~TileEditor() {
  delete ui;
}

void TileEditor::done(int r) {
  if (r == QDialog::Accepted) {
    obj["id"] = ui->id->text().toInt();
    bool isRef;
    auto ref = ui->name->text().toInt(&isRef);
    obj.remove("ref");
    obj.remove("name");
    if (isRef) {
      obj["ref"] = ref;
    } else if (!ui->name->text().isEmpty()) {
      obj["name"] = ui->name->text();
    }
    if (ui->color->text().isEmpty()) {
      obj.remove("color");
    } else {
      obj["color"] = ui->color->text();
    }
    int flags = 0;
    flags |= ui->solid->isChecked() ? 1 : 0;
    flags |= ui->trans->isChecked() ? 2 : 0;
    flags |= ui->dirt->isChecked() ? 4 : 0;
    flags |= ui->stone->isChecked() ? 8 : 0;
    flags |= ui->grass->isChecked() ? 0x10 : 0;
    flags |= ui->pile->isChecked() ? 0x20 : 0;
    flags |= ui->flip->isChecked() ? 0x40 : 0;
    flags |= ui->brick->isChecked() ? 0x80 : 0;
    flags |= ui->moss->isChecked() ? 0x100 : 0;
    flags |= ui->mergef->isChecked() ? 0x200 : 0;
    flags |= ui->large->isChecked() ? 0x400 : 0;
    if (flags == 0) {
      obj.remove("flags");
    } else {
      obj["flags"] = flags;
    }
    if (ui->merge->text().isEmpty()) {
      obj.remove("merge");
    } else {
      obj["merge"] = ui->merge->text();
    }
    if (ui->blend->text().isEmpty()) {
      obj.remove("blend");
    } else {
      obj["blend"] = ui->blend->text();
    }
    if (ui->skipy->value() == 0) {
      obj.remove("skipy");
    } else {
      obj["skipy"] = ui->skipy->value();
    }
    if (ui->toppad->value() == 0) {
      obj.remove("toppad");
    } else {
      obj["toppad"] = ui->toppad->value();
    }
    if (ui->w->value() == 18) {
      obj.remove("w");
    } else {
      obj["w"] = ui->w->value();
    }
    if (ui->h->value() == 18) {
      obj.remove("h");
    } else {
      obj["h"] = ui->h->value();
    }
    if (ui->red->value() == 0.0) {
      obj.remove("r");
    } else {
      obj["r"] = ui->red->value();
    }
    if (ui->green->value() == 0.0) {
      obj.remove("g");
    } else {
      obj["g"] = ui->green->value();
    }
    if (ui->blue->value() == 0.0) {
      obj.remove("b");
    } else {
      obj["b"] = ui->blue->value();
    }
  }
  QDialog::done(r);
}

void TileEditor::changeColor() {
  QColor orig = QColor(obj["color"].toString("#000000"));
  auto color = QColorDialog::getColor(orig, this);
  if (color.isValid()) {
    ui->color->setText(color.name());
  }
}
