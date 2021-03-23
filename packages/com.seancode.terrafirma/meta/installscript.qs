function Component()
{
}

Component.prototype.createOperations = function()
{
  component.createOperations();
  if (systemInfo.productType == "windows") {
    component.addOperation("CreateShortcut", "@TargetDir@/terrafirma.exe",
      "@StartMenuDir@/Terrafirma.lnk", "workingDirectory=@TargetDir@");
  }
}
