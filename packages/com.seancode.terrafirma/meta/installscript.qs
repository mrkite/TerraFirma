var targetDirectoryPage = null;

function Component()
{
  installer.gainAdminRights();
  component.loaded.connect(this, this.installerLoaded);
}

Component.prototype.createOperations = function()
{
  component.createOperations();
  if (systemInfo.productType == "windows") {
    component.addOperation("CreateShortcut",
                           "@TargetDir@/terrafirma.exe",
                           "@DesktopDir@/Terrafirma.lnk",
                           "workingDirectory=@TargetDir@");
    component.addOperation("CreateShortcut",
                           "@TargetDir@/terrafirma.exe",
                           "@StartMenuDir@/Terrafirma.lnk",
                           "workingDirectory=@TargetDir@");
  }
}

Component.prototype.installerLoaded = function()
{
  installer.setDefaultPageVisible(QInstaller.TargetDirectory, false);
  installer.addWizardPage(component, "TargetWidget", QInstaller.TargetDirectory);

  targetDirectoryPage = gui.pageWidgetByObjectName("DynamicTargetWidget");
  targetDirectoryPage.windowTitle = "Choose Installation Directory";
  targetDirectoryPage.description.setText("Please select where TerraFirma will be installed:");
  targetDirectoryPage.targetDirectory.textChanged.connect(this, this.targetDirectoryChanged);
  targetDirectoryPage.targetDirectory.setText(installer.value("TargetDir"));
  targetDirectoryPage.targetChooser.released.connect(this, this.targetChooserClicked);

  gui.pageById(QInstaller.ComponentSelection).entered.connect(this, this.componentSelectionPageEntered);
}

Component.prototype.targetChooserClicked = function()
{
  var dir = QFileDialog.getExistingDirectory("", targetDirectoryPage.targetDirectory.text);
  targetDirectoryPage.targetDirectory.setText(dir);
}

Component.prototype.targetDirectoryChanged = function()
{
  var dir = targetDirectoryPage.targetDirectory.text;
  if (installer.fileExists(dir) && installer.fileExists(dir + "/maintenancetool.exe")) {
    targetDirectoryPage.warning.setText("<p style=\"color: red\">Existing installation detected and will be overwritten.</p>");
  } else if (installer.fileExists(dir)) {
    targetDirectory.warning.setText("<p style=\"color: red\">Installing in existing directory. It will be wiped on uninstallation.</p>");
  } else {
    targetDirectoryPage.warning.setText("");
  }
  installer.setValue("TargetDir", dir);
}

Component.prototype.componentSelectionPageEntered = function()
{
  var dir = installer.value("TargetDir");
  if (installer.fileExists(dir) && installer.fileExists(dir + "/maintenancetool.exe")) {
    installer.execute(dir + "/maintenancetool.exe", ["purge"], "yes");
  }
}
