/**
 * @Copyright 2015 seancode
 *
 * Handles chest popup
 */

#include "./chestview.h"
#include "./ui_chestview.h"

ChestView::ChestView(QString name, const QList<QString> &items,
                     QWidget *parent)
  : QWidget(parent), ui(new Ui::ChestView) {
  ui->setupUi(this);

  setWindowFlags(Qt::Popup);

  ui->label->setText(name);

  model = new QStringListModel(items, this);
  ui->listView->setModel(model);
}

ChestView::~ChestView() {
  delete ui;
}
