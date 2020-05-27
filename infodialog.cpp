/**
 * @Copyright 2015 seancode
 *
 * Handles world information dialog
 */

#include "./infodialog.h"
#include "./ui_infodialog.h"

InfoDialog::InfoDialog(const WorldHeader &header, QWidget *parent)
  : QDialog(parent), ui(new Ui::InfoDialog) {
  ui->setupUi(this);

  model = new QStandardItemModel(39, 2, this);
  ui->tableView->setModel(model);

  curRow = 0;

  add("World Type", header.is("crimson") ? "Crimson" : "Corruption");
  add("Eye of Cthulhu", header.is("killedBoss1") ? "Blackened" : "Undefeated");
  // killedBoss2 should be Brain of Cthulhu in crimson, but isn't
  if (!header.is("crimson"))
    add("Eater of Worlds", header.is("killedBoss2") ? "Choked" : "Undefeated");
  add("Skeletron", header.is("killedBoss3") ? "Boned" : "Undefeated");
  add("Wall of Flesh", header.is("hardMode") ? "Flayed" : "Undefeated");
  add("Queen Bee", header.is("killedQueenBee") ? "Swatted" : "Undefeated");
  add("The Destroyer",
      header.is("killedMechBoss1") ? "Destroyed" : "Undefeated");
  add("The Twins", header.is("killedMechBoss2") ? "Separated" : "Undefeated");
  add("Skeletron Prime",
      header.is("killedMechBoss3") ? "Deboned" : "Undefeated");
  add("Plantera", header.is("killedPlantBoss") ? "Weeded" : "Undefeated");
  add("Golem", header.is("killedGolemBoss") ? "Stoned" : "Undefeated");
  add("Duke Fishron", header.is("downedFishron") ? "Filleted" : "Undefeated");
  add("King Slime", header.is("killedSlimeKing") ? "Ninja'd" : "Undefeated");
  add("Goblin Invasion",
      header.is("killedGoblins") ? "Thwarted" : "Undefeated");
  add("Clown", header.is("killedClown") ? "Eviscerated" : " Undefeated");
  add("Frost Horde", header.is("killedFrost") ? "Thawed" : "Undefeated");
  add("Pirates", header.is("killedPirates") ? "Keelhauled" : "Undefeated");
  add("Pumpking", header.is("downedHalloweenKing") ? "Carved" : "Undefeated");
  add("Mourning Wood",
      header.is("downedHalloweenTree") ? "Whittled" : "Undefeated");
  add("Ice Queen",
      header.is("downedChristmasIceQueen") ? "Melted" : "Undefeated");
  add("Santa-NK1",
      header.is("downedChristmasSantank") ? "Sleighn" : "Undefeated");
  add("Everscream",
      header.is("downedChristmasTree") ? "Silenced" : "Undefeated");
  add("Martian Invasion",
      header.is("downedMartians") ? "Area 51ed" : "Undefeated");
  add("Cultists",
      header.is("downedAncientCultist") ? "Indoctrinated" : "Undefeated");
  add("Solar Pillar", header.is("downedSolar") ? "Eclipsed" : "Undefeated");
  add("Vortex Pillar", header.is("downedVortex") ? "Twisted" : "Undefeated");
  add("Nebula Pillar", header.is("downedNebula") ? "Accreted" : "Undefeated");
  add("Stardust Pillar",
      header.is("downedStardust") ? "Scattered" : "Undefeated");
  add("Moon Lord", header.is("downedMoonlord") ? "Dethroned" : "Undefeated");
  add("Tinkerer", header.is("savedTinkerer") ?
      "Saved" : header.is("killedGoblins") ?
      "Bound" : "Not present yet");
  add("Wizard", header.is("savedWizard") ?
      "Saved" : header.is("hardMode") ?
      "Bound" : "Not present yet");
  add("Mechanic", header.is("savedMechanic") ?
      "Saved" : header.is("killedBoss3") ?
      "Bound" : "Not present yet");
  add("Angler", header.is("savedAngler") ? "Saved" : "Sleeping");
  add("Stylist", header.is("savedStylist") ? "Saved" : "Webbed");
  add("Tax Collector", header.is("savedTaxCollector") ?
      "Saved" : header.is("hardMode") ?
      "Tortured" : "Not present yet");
  add("Game Mode", header.is("lunarApocalypse") ?
      "Lunar" : header.is("hardMode") ? "Hard" : "Normal");
  add("World Mode", header.is("expert") ? "Expert" : "Normal");
  add("Orbs left til EoW",
      QString("%1").arg(3 - header["shadowOrbCount"]->toInt()));
  add("Altars Smashed", QString("%1").arg(header["altarsSmashed"]->toInt()));
}

InfoDialog::~InfoDialog() {
  delete ui;
}

void InfoDialog::add(const QString &key, const QString &val) {
  model->setData(model->index(curRow, 0), key, Qt::DisplayRole);
  model->setData(model->index(curRow++, 1), val, Qt::DisplayRole);
}
