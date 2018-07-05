![alt text](https://raw.githubusercontent.com/Moolt/SmartReplace/master/logo.png "logo")

# About
Smart.Replace is a Unity Plugin for restoring broken prefab references in scenes or replacing them completely.
All values, references on other objects and external references on the replaced objects are transferred onto the newly instantiated prefab.

# Requirements
In order to use the plugin the corrent .NET version is required to be set for your project. **Otherwise it will not compile**.
You can change the .NET version via: ```File -> Build settings -> Other settings -> Configuration -> Scripting Runtime Version -> .NET 4.x Equivalent```

# Limitations
1. As Smart.Replace is agnostic about object hierarchies, only one component per type will be transferred. This enables the plugin to transfer a component's value even when the object hierarchy of the newly instantiated object differs from the replaced object.

2. External ```GameObject``` references to the object being replaced will be set to the parent object of the instantiated prefab.

# Usage
Smart.Replace can be opened via ```Window -> Replace Prefab...```
Everything else should be self explanatory.
