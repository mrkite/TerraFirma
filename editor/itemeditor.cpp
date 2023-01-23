/** @copyright 2023 Sean Kasun */
#include "itemeditor.h"
#include "ui_itemeditor.h"

ItemEditor::ItemEditor(QJsonObject obj, QWidget *parent) : QDialog(parent), ui(new Ui::ItemEditor), obj(obj) {
  ui->setupUi(this);
  ui->id->setText(tr("%1").arg(obj["id"].toInt()));
  ui->name->setText(obj["name"].toString());
}

ItemEditor::~ItemEditor() {
  delete ui;
}

void ItemEditor::done(int r) {
  if (r == QDialog::Accepted) {
    obj["id"] = ui->id->text().toInt();
    obj["name"] = ui->name->text();
  }
  QDialog::done(r);
}
