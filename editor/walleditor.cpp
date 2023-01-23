/** @copyright 2023 Sean Kasun */
#include <QColorDialog>
#include "walleditor.h"
#include "ui_walleditor.h"

WallEditor::WallEditor(QJsonObject obj, QWidget *parent) : QDialog(parent), ui(new Ui::WallEditor), obj(obj) {
  ui->setupUi(this);
  ui->id->setText(tr("%1").arg(obj["id"].toInt()));
  if (obj.contains("ref")) {
    ui->name->setText(tr("%1").arg(obj["ref"].toInt()));
  } else {
    ui->name->setText(obj["name"].toString());
  }
  ui->color->setText(obj["color"].toString());
  if (obj.contains("blend")) {
    ui->blend->setText(tr("%1").arg(obj["blend"].toInt()));
  }
  if (obj.contains("large")) {
    ui->large->setText(tr("%1").arg(obj["large"].toInt()));
  }
}

WallEditor::~WallEditor() {
  delete ui;
}

void WallEditor::done(int r) {
  if (r == QDialog::Accepted) {
    obj["id"] = ui->id->text().toInt();
    bool isRef;
    auto ref = ui->name->text().toInt(&isRef);
    obj.remove("ref");
    obj.remove("name");
    if (isRef) {
      obj["ref"] = ref;
    } else {
      obj["name"] = ui->name->text();
    }
    if (ui->color->text().isEmpty()) {
      obj.remove("color");
    } else {
      obj["color"] = ui->color->text();
    }
    if (ui->blend->text().isEmpty()) {
      obj.remove("blend");
    } else {
      obj["blend"] = ui->blend->text().toInt();
    }
    if (ui->large->text().isEmpty()) {
      obj.remove("large");
    } else {
      obj["large"] = ui->large->text().toInt();
    }
  }
  QDialog::done(r);
}

void WallEditor::changeColor() {
  QColor orig = QColor(obj["color"].toString("#000000"));
  auto color = QColorDialog::getColor(orig, this);
  if (color.isValid()) {
    ui->color->setText(color.name());
  }
}
