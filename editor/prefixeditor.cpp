/** @copyright 2023 Sean Kasun */
#include "prefixeditor.h"
#include "ui_prefixeditor.h"

PrefixEditor::PrefixEditor(QJsonObject obj, QWidget *parent) : QDialog(parent), ui(new Ui::PrefixEditor), obj(obj) {
  ui->setupUi(this);
  ui->id->setText(tr("%1").arg(obj["id"].toInt()));
  ui->name->setText(obj["name"].toString());
}

PrefixEditor::~PrefixEditor() {
  delete ui;
}

void PrefixEditor::done(int r) {
  if (r == QDialog::Accepted) {
    obj["id"] = ui->id->text().toInt();
    obj["name"] = ui->name->text();
  }
  QDialog::done(r);
}
