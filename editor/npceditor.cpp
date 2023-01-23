/** @copyright 2023 Sean Kasun */
#include "npceditor.h"
#include "ui_npceditor.h"

NPCEditor::NPCEditor(QJsonObject obj, QWidget *parent) : QDialog(parent), ui(new Ui::NPCEditor), obj(obj) {
  ui->setupUi(this);
  ui->id->setText(tr("%1").arg(obj["id"].toInt()));
  ui->name->setText(obj["name"].toString());
  if (obj.contains("head")) {
    ui->head->setText(tr("%1").arg(obj["head"].toInt()));
  }
  if (obj.contains("banner")) {
    ui->banner->setText(tr("%1").arg(obj["banner"].toInt()));
  }
}

NPCEditor::~NPCEditor() {
  delete ui;
}

void NPCEditor::done(int r) {
  if (r == QDialog::Accepted) {
    obj["id"] = ui->id->text().toInt();
    obj["name"] = ui->name->text();
    if (ui->head->text().isEmpty()) {
      obj.remove("head");
    } else {
      obj["head"] = ui->head->text().toInt();
    }
    if (ui->banner->text().isEmpty()) {
      obj.remove("banner");
    } else {
      obj["banner"] = ui->banner->text().toInt();
    }
  }
  QDialog::done(r);
}
