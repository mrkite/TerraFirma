/** @copyright 2023 Sean Kasun */
#include <QColorDialog>
#include "tilevareditor.h"
#include "ui_tilevareditor.h"

TileVarEditor::TileVarEditor(QJsonObject obj, QWidget *parent) : QDialog(parent), ui(new Ui::TileVarEditor), obj(obj) {
  ui->setupUi(this);
  if (obj.contains("x")) {
    ui->x->setText(tr("%1").arg(obj["x"].toInt()));
  } else if (obj.contains("minx")) {
    ui->x->setText(tr(">%1").arg(obj["minx"].toInt()));
  } else if (obj.contains("maxx")) {
    ui->x->setText(tr("<%1").arg(obj["maxx"].toInt()));
  }
  if (obj.contains("y")) {
    ui->y->setText(tr("%1").arg(obj["y"].toInt()));
  } else if (obj.contains("miny")) {
    ui->y->setText(tr(">%1").arg(obj["miny"].toInt()));
  } else if (obj.contains("maxy")) {
    ui->y->setText(tr("<%1").arg(obj["maxy"].toInt()));
  }
  if (obj.contains("ref")) {
    ui->name->setText(tr("%1").arg(obj["ref"].toInt()));
  } else {
    ui->name->setText(obj["name"].toString());
  }
  ui->color->setText(obj["color"].toString());
  ui->toppad->setValue(obj["toppad"].toInt());
  ui->w->setValue(obj["w"].toInt(18));
  ui->h->setValue(obj["h"].toInt(18));
  ui->red->setValue(obj["r"].toDouble());
  ui->green->setValue(obj["g"].toDouble());
  ui->blue->setValue(obj["b"].toDouble());
}

TileVarEditor::~TileVarEditor() {
  delete ui;
}

void TileVarEditor::done(int r) {
  if (r == QDialog::Accepted) {
    QString x = ui->x->text();
    obj.remove("x");
    obj.remove("minx");
    obj.remove("maxx");
    if (x.startsWith(">")) {
      x.remove(0, 1);
      obj["minx"] = x.toInt();
    } else if (x.startsWith("<")) {
      x.remove(0, 1);
      obj["maxx"] = x.toInt();
    } else if (!x.isEmpty()){
      obj["x"] = x.toInt();
    }
    QString y = ui->y->text();
    obj.remove("y");
    obj.remove("miny");
    obj.remove("maxy");
    if (y.startsWith(">")) {
      y.remove(0, 1);
      obj["miny"] = y.toInt();
    } else if (y.startsWith("<")) {
      y.remove(0, 1);
      obj["maxy"] = y.toInt();
    } else if (!y.isEmpty()) {
      obj["y"] = y.toInt();
    }
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

void TileVarEditor::changeColor() {
  QColor orig = QColor(obj["color"].toString("#000000"));
  auto color = QColorDialog::getColor(orig, this);
  if (color.isValid()) {
    ui->color->setText(color.name());
  }
}
