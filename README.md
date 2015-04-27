# azurerm-param-tool
Lightweight tool for editing azure rm template parameters from command line.

#Example Usage
* `azurermparamtool /File:azurerm.param.json /List` - This will list all configurable parameters in the file "azurerm.param.json".
* `azurermparamtool /File:azurerm.param.json /Param:adminUser /Val:tommy` - This will set the parameter "adminUser" to "tommy" in the file "azurerm.param.json".

#More Example Usage
* `azurermparamtool /List` - If the file "override.param.json" exists next to the .exe it will use that instead. Lists all the configurable parameter.
* `azurermparamtool /Param:adminUser /Val:tommy` - If the file "override.param.json" exists next to the .exe it will use that instead. his will set the parameter "adminUser" to "tommy".
