/** @copyright 2023 Sean Kasun */
#include "headereditor.h"
#include "ui_headereditor.h"

HeaderEditor::HeaderEditor(QJsonObject obj, QWidget *parent) : QDialog(parent), ui(new Ui::HeaderEditor), obj(obj) {
  ui->setupUi(this);
  ui->name->setText(obj["name"].toString());
  ui->type->setCurrentText(obj["type"].toString());
  if (obj.contains("num")) {
    ui->array->setText(tr("%1").arg(obj["num"].toInt()));
  } else {
    ui->array->setText(obj["relnum"].toString());
  }
  ui->min->setText(tr("%1").arg(obj["min"].toInt(88)));
}

HeaderEditor::~HeaderEditor() {
  delete ui;
}

void HeaderEditor::done(int r) {
  if (r == QDialog::Accepted) {
    obj["name"] = ui->name->text();
    obj["type"] = ui->type->currentText();
    obj.remove("num");
    obj.remove("relnum");
    bool isNum;
    auto num = ui->array->text().toInt(&isNum);
    if (isNum) {
      obj["num"] = num;
    } else if (!ui->array->text().isEmpty()) {
      obj["relnum"] = ui->array->text();
    }
    auto min = ui->min->text().toInt();
    if (min == 88) {
      obj.remove("min");
    } else {
      obj["min"] = min;
    }
  }
  QDialog::done(r);
}
