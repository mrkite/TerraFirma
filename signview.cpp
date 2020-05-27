/**
 * @Copyright 2015 seancode
 *
 * Handles display of the sign popup
 */

#include "./signview.h"
#include "./ui_signview.h"

SignView::SignView(const QString &text, QWidget *parent) : QWidget(parent),
  ui(new Ui::SignView) {
  setWindowFlags(Qt::Popup);

  ui->setupUi(this);

  ui->label->setText(text);
}

SignView::~SignView() {
  delete ui;
}
