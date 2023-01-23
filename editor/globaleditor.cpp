/** @copyright 2023 Sean Kasun */
#include <QColorDialog>
#include "globaleditor.h"
#include "ui_globaleditor.h"

GlobalEditor::GlobalEditor(QJsonObject obj, QWidget *parent) : QDialog(parent), ui(new Ui::GlobalEditor), obj(obj) {
  ui->setupUi(this);
  ui->id->setText(obj["id"].toString());
  ui->color->setText(obj["color"].toString());
}

GlobalEditor::~GlobalEditor() {
  delete ui;
}

void GlobalEditor::changeColor() {
  QColor orig = QColor(obj["color"].toString("#000000"));
  auto color = QColorDialog::getColor(orig, this);
  if (color.isValid()) {
    ui->color->setText(color.name());
  }
}

void GlobalEditor::done(int r) {
  if (r == QDialog::Accepted) {
    obj["id"] = ui->id->text();
    obj["color"] = ui->color->text();
  }
  QDialog::done(r);
}
