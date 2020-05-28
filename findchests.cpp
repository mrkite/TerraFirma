/**
 * @Copyright 2015 seancode
 *
 * @Handles item list dialog
 */

#include "./findchests.h"
#include "./ui_findchests.h"


class FindChests::ItemsFilterProxyModel : public QSortFilterProxyModel {
  public:
    explicit ItemsFilterProxyModel(QObject* parent = nullptr)
    : QSortFilterProxyModel(parent) {
    }

  bool filterAcceptsRow(int sourceRow,
                        const QModelIndex &sourceParent) const override {
    if (sourceParent.isValid())
      return true;

    return QSortFilterProxyModel::filterAcceptsRow(sourceRow, sourceParent);
  }
};


FindChests::FindChests(const QList<World::Chest> &chests, L10n *l10n, QWidget *parent)
  : QDialog(parent), ui(new Ui::FindChests) {
  ui->setupUi(this);

  QHash<QString, QList<int>> roots;

  for (int i = 0; i < chests.length(); i++) {
    for (auto const &item : chests[i].items) {
      if (!roots[l10n->xlateItem(item.name)].contains(i))
        roots[l10n->xlateItem(item.name)].append(i);
    }
  }

  QHashIterator<QString, QList<int>> i(roots);
  auto root = model.invisibleRootItem();
  while (i.hasNext()) {
    i.next();
    auto item = new QStandardItem(i.key());
    item->setEditable(false);
    for (int num : i.value()) {
      auto child = new QStandardItem(chests[num].name.isEmpty() ?
                                     tr("Chest #%1").arg(num) : chests[num].name);
      child->setEditable(false);
      child->setData(QPointF(chests[num].x, chests[num].y), Qt::UserRole);
      item->appendRow(child);
    }
    root->appendRow(item);
  }

  model.sort(0, Qt::AscendingOrder);

  filter = new ItemsFilterProxyModel(this);
  filter->setSourceModel(&model);
  ui->treeView->setModel(filter);

  connect(ui->treeView->selectionModel(), &QItemSelectionModel::currentChanged,
          this, &FindChests::chestSelected);
}

FindChests::~FindChests() {
  delete ui;
}

void FindChests::chestSelected(QModelIndex const& current, QModelIndex const& previous) {
  (void)previous;

  if (current.isValid()) {
    auto data = current.data(Qt::UserRole);
    if (!data.isNull())
      emit jump(data.toPointF());
  }
}

void FindChests::searchTextChanged(const QString &newText) {
  filter->setFilterRegExp(newText);
  filter->setFilterCaseSensitivity(Qt::CaseInsensitive);
}
