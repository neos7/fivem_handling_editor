﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using MenuAPI;

namespace HandlingEditor.Client
{
    public class HandlingMenu : BaseScript
    {
        #region Editor Properties

        public string ScriptName => HandlingEditor.ScriptName;
        public string kvpPrefix => HandlingEditor.kvpPrefix;
        public float FloatStep => HandlingEditor.FloatStep;
        public int ToggleMenu => HandlingEditor.ToggleMenu;
        public bool CurrentPresetIsValid => HandlingEditor.CurrentPresetIsValid;
        public HandlingPreset CurrentPreset => HandlingEditor.CurrentPreset;
        public Dictionary<string, HandlingPreset> ServerPresets => HandlingEditor.ServerPresets;

        #endregion

        #region Menu Fields

        public MenuController menuController;
        public Menu EditorMenu;
        public Menu PersonalPresetsMenu;
        public Menu ServerPresetsMenu;
        public List<MenuDynamicListItem> HandlingListItems;

        #endregion

        #region Events

        public static event EventHandler ResetPresetButtonPressed;
        public static event EventHandler<string> ApplyPersonalPresetButtonPressed;
        public static event EventHandler<string> ApplyServerPresetButtonPressed;
        public static event EventHandler<string> SavePersonalPresetButtonPressed;
        public static event EventHandler<string> SaveServerPresetButtonPressed;
        public static event EventHandler<string> DeletePersonalPresetButtonPressed;
        public static event EventHandler<string> DeleteServerPresetButtonPressed;

        #endregion

        #region Constructor

        public HandlingMenu()
        {
            Tick += OnTick;
            HandlingEditor.PresetChanged += new EventHandler((sender,args) => InitializeMenu());
            HandlingEditor.PersonalPresetsListChanged += new EventHandler((sender,args) => UpdatePersonalPresetsMenu());
            HandlingEditor.ServerPresetsListChanged += new EventHandler((sender,args) => UpdateServerPresetsMenu());
        }

        #endregion

        #region Tasks
        
        private async Task OnTick()
        {
            if (!CurrentPresetIsValid)
            {
                if (MenuController.IsAnyMenuOpen())
                    MenuController.CloseAllMenus();
            }
        }
        
        #endregion

        #region Menu Methods

        private void InitializeMenu()
        {
            if (EditorMenu == null)
            {
                EditorMenu = new Menu(ScriptName, "Editor");

                EditorMenu.OnItemSelect += EditorMenu_OnItemSelect;
                EditorMenu.OnMenuDynamicListItemCurrentItemChange += EditorMenu_OnMenuDynamicListItemCurrentItemChange;
            }
            
            if (PersonalPresetsMenu == null)
            {
                PersonalPresetsMenu = new Menu(ScriptName, "Personal Presets");

                PersonalPresetsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
                PersonalPresetsMenu.InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

                // Disable Controls binded on the same key
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.SelectWeapon, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.VehicleExit, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));
                
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneExtraOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control> (async (sender, control) =>
                {
                    string kvpName = await GetOnScreenString("");
                    SavePersonalPresetButtonPressed(PersonalPresetsMenu, kvpName);
                }) , true));
                PersonalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) =>
                {
                    if (PersonalPresetsMenu.GetMenuItems().Count > 0)
                    {
                        string kvpName = PersonalPresetsMenu.GetMenuItems()[PersonalPresetsMenu.CurrentIndex].Text;
                        DeletePersonalPresetButtonPressed(PersonalPresetsMenu, kvpName);
                    }
                }), true));

                PersonalPresetsMenu.OnItemSelect += (sender, item, index) =>
                {
                    ApplyPersonalPresetButtonPressed.Invoke(sender, item.Text);

                    UpdateEditorMenu();
                };

            }
            if (ServerPresetsMenu == null)
            {
                ServerPresetsMenu = new Menu(ScriptName, "Server Presets");
                
                ServerPresetsMenu.OnItemSelect += (sender, item, index) =>
                {
                    ApplyServerPresetButtonPressed.Invoke(sender, item.Text);

                    
                    UpdateEditorMenu();

                };
            }

            UpdatePersonalPresetsMenu();
            UpdateServerPresetsMenu();
            UpdateEditorMenu();

            if (menuController == null)
            {
                menuController = new MenuController();
                MenuController.AddMenu(EditorMenu);
                MenuController.AddMenu(PersonalPresetsMenu);
                MenuController.AddMenu(ServerPresetsMenu);
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.MenuToggleKey = (Control)ToggleMenu;
                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.MainMenu = EditorMenu;
            }
        }

        private void UpdateEditorMenu()
        {
            if (EditorMenu == null)
                return;

            EditorMenu.ClearMenuItems();

            // Create Personal Presets sub menu and bind item to a button
            var PersonalPresetsItem = new MenuItem("Personal Presets", "The handling presets saved by you.")
            {
                Label = "→→→"
            };
            EditorMenu.AddMenuItem(PersonalPresetsItem);
            MenuController.BindMenuItem(EditorMenu, PersonalPresetsMenu, PersonalPresetsItem);

            // Create Server Presets sub menu and bind item to a button
            var ServerPresetsItem = new MenuItem("Server Presets", "The handling presets loaded from the server.")
            {
                Label = "→→→"
            };
            EditorMenu.AddMenuItem(ServerPresetsItem);
            MenuController.BindMenuItem(EditorMenu, ServerPresetsMenu, ServerPresetsItem);

            // Add all the controllers
            foreach (var item in HandlingInfo.FieldsInfo)
            {
                var fieldInfo = item.Value;

                if (fieldInfo.Editable)
                {

                    //string fieldName = fieldInfo.Name;
                    //string fieldDescription = fieldInfo.Description;
                    Type fieldType = fieldInfo.Type;

                    if (fieldType == FieldType.FloatType)
                        AddDynamicFloatList(EditorMenu, (FieldInfo<float>)item.Value);
                    else if (fieldType == FieldType.IntType)
                        AddDynamicIntList(EditorMenu, (FieldInfo<int>)item.Value);
                    else if (fieldType == FieldType.Vector3Type)
                        AddDynamicVector3List(EditorMenu, (FieldInfo<Vector3>)item.Value);
                }
                else
                {
                    AddLockedItem(EditorMenu, item.Value);
                }
            }

            var resetItem = new MenuItem("Reset", "Restores the default values")
            {
                ItemData = "handling_reset",
            };
            EditorMenu.AddMenuItem(resetItem);

            //UpdatePersonalPresetsMenu();
            //UpdateServerPresetsMenu();
        }

        private async void EditorMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
        {
            if (menu != EditorMenu)
                return;

            if (menuItem is MenuDynamicListItem dynamicListItem)
            {
                if (!(dynamicListItem.ItemData is BaseFieldInfo fieldInfo))
                    return;

                var currentItem = dynamicListItem.CurrentItem;
                var itemText = dynamicListItem.Text;
                string fieldName = fieldInfo.Name;
                var fieldType = fieldInfo.Type;

                string text = await GetOnScreenString(currentItem);

                if (fieldType == FieldType.FloatType)
                {
                    var min = (fieldInfo as FieldInfo<float>).Min;
                    var max = (fieldInfo as FieldInfo<float>).Max;

                    if (float.TryParse(text, out float newvalue))
                    {
                        if (newvalue >= min && newvalue <= max)
                        {
                            dynamicListItem.CurrentItem = newvalue.ToString();
                            // TODO: Move outside UI 
                            CurrentPreset.Fields[fieldName] = newvalue;
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldName}~w~, Min:{min}, Max:{max}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldName}~w~");
                }
                else if (fieldType == FieldType.IntType)
                {
                    var min = (fieldInfo as FieldInfo<int>).Min;
                    var max = (fieldInfo as FieldInfo<int>).Max;

                    if (int.TryParse(text, out int newvalue))
                    {
                        if (newvalue >= min && newvalue <= max)
                        {
                            dynamicListItem.CurrentItem = newvalue.ToString();
                            // TODO: Move outside UI 
                            CurrentPreset.Fields[fieldName] = newvalue;
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{fieldName}~w~, Min:{min}, Max:{max}");
                    }
                    else
                        Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{fieldName}~w~");
                }
                else if (fieldType == FieldType.Vector3Type)
                {
                    var min = (fieldInfo as FieldInfo<Vector3>).Min;
                    var max = (fieldInfo as FieldInfo<Vector3>).Max;

                    var minValueX = min.X;
                    var minValueY = min.Y;
                    var minValueZ = min.Z;
                    var maxValueX = max.X;
                    var maxValueY = max.Y;
                    var maxValueZ = max.Z;

                    if (itemText.EndsWith("_x"))
                    {
                        if (float.TryParse(text, out float newvalue))
                        {
                            if (newvalue >= minValueX && newvalue <= maxValueX)
                            {
                                dynamicListItem.CurrentItem = newvalue.ToString();
                                // TODO: Move outside UI 
                                CurrentPreset.Fields[fieldName].X = newvalue;
                            }
                            else
                                Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueX}, Max:{maxValueX}");
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{itemText}~w~");
                    }
                    else if (itemText.EndsWith("_y"))
                    {
                        if (float.TryParse(text, out float newvalue))
                        {
                            if (newvalue >= minValueY && newvalue <= maxValueY)
                            {
                                dynamicListItem.CurrentItem = newvalue.ToString();
                                // TODO: Move outside UI 
                                CurrentPreset.Fields[fieldName].Y = newvalue;
                            }
                            else
                                Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueY}, Max:{maxValueY}");
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{itemText}~w~");
                    }
                    else if (itemText.EndsWith("_z"))
                    {
                        if (float.TryParse(text, out float newvalue))
                        {
                            if (newvalue >= minValueZ && newvalue <= maxValueZ)
                            {
                                dynamicListItem.CurrentItem = newvalue.ToString();
                                // TODO: Move outside UI 
                                CurrentPreset.Fields[fieldName].Z = newvalue;
                            }
                            else
                                Screen.ShowNotification($"{ScriptName}: Value out of allowed limits for ~b~{itemText}~w~, Min:{minValueZ}, Max:{maxValueZ}");
                        }
                        else
                            Screen.ShowNotification($"{ScriptName}: Invalid value for ~b~{itemText}~w~");
                    }
                }
            }
            else if ((menuItem.ItemData as string) == "handling_reset")
            {
                ResetPresetButtonPressed(this, EventArgs.Empty);
            }
        }

        private void EditorMenu_OnMenuDynamicListItemCurrentItemChange(Menu menu, MenuDynamicListItem dynamicListItem, string oldValue, string newValue)
        {
            if (menu != EditorMenu)
                return;

            if (!(dynamicListItem.ItemData is BaseFieldInfo fieldInfo))
                return;

            // Get field name which is controlled by this dynamic list item
            string fieldName = fieldInfo.Name;
            var text = dynamicListItem.Text;
            var fieldType = fieldInfo.Type;

            if (fieldType == FieldType.FloatType)
                CurrentPreset.Fields[fieldName] = float.Parse(newValue);
            else if (fieldType == FieldType.IntType)
                CurrentPreset.Fields[fieldName] = int.Parse(newValue);
            else if (fieldType == FieldType.Vector3Type)
            {
                if(text.EndsWith("_x"))
                    CurrentPreset.Fields[fieldName].X = float.Parse(newValue);
                else if (text.EndsWith("_y"))
                    CurrentPreset.Fields[fieldName].Y = float.Parse(newValue);
                else if (text.EndsWith("_z"))
                    CurrentPreset.Fields[fieldName].Z = float.Parse(newValue);
            }

        }

        private void UpdatePersonalPresetsMenu()
        {
            if (PersonalPresetsMenu == null)
                return;
            PersonalPresetsMenu.ClearMenuItems();

            KvpEnumerable kvpList = new KvpEnumerable(kvpPrefix);
            foreach (var key in kvpList)
            {
                string value = GetResourceKvpString(key);
                PersonalPresetsMenu.AddMenuItem(new MenuItem(key.Remove(0, kvpPrefix.Length)));
            }
        }

        private void UpdateServerPresetsMenu()
        {
            if (ServerPresetsMenu == null)
                return;

            ServerPresetsMenu.ClearMenuItems();

            foreach (var preset in ServerPresets)
                ServerPresetsMenu.AddMenuItem(new MenuItem(preset.Key));
        }

        private async Task<string> GetOnScreenString(string defaultText)
        {
            var currentMenu = MenuController.GetCurrentMenu();
            currentMenu.Visible = false;

            MenuController.DisableMenuButtons = true;
            DisableAllControlActions(1);

            AddTextEntry("HANDLING_EDITOR_ENTER_VALUE", "Enter value (without spaces)");
            DisplayOnscreenKeyboard(1, "HANDLING_EDITOR_ENTER_VALUE", "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await Delay(200);

            EnableAllControlActions(1);
            MenuController.DisableMenuButtons = false;
            currentMenu.Visible = true;

            return GetOnscreenKeyboardResult();
        }

        string DynamicListChangeCallback(MenuDynamicListItem item, bool left)
        {
            var currentItem = item.CurrentItem;

            if (!(item.ItemData is BaseFieldInfo fieldInfo))
                return currentItem;

            var itemText = item.Text;
            var fieldName = fieldInfo.Name;
            var fieldType = fieldInfo.Type;

            if (fieldType == FieldType.IntType)
            {
                var value = int.Parse(currentItem);
                var min = (fieldInfo as FieldInfo<int>).Min;
                var max = (fieldInfo as FieldInfo<int>).Max;

                if (left)
                {
                    var newvalue = value - 1;
                    if (newvalue < min)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{fieldName}~w~ is {min}");
                    else
                    {
                        value = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + 1;
                    if (newvalue > max)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{fieldName}~w~ is {max}");
                    else
                    {
                        value = newvalue;
                    }
                }
                return value.ToString();
            }
            else if (fieldType == FieldType.FloatType)
            {
                var value = float.Parse(currentItem);
                var min = (fieldInfo as FieldInfo<float>).Min;
                var max = (fieldInfo as FieldInfo<float>).Max;

                if (left)
                {
                    var newvalue = value - FloatStep;
                    if (newvalue < min)
                        Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{fieldName}~w~ is {min}");
                    else
                    {
                        value = newvalue;
                    }
                }
                else
                {
                    var newvalue = value + FloatStep;
                    if (newvalue > max)
                        Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{fieldName}~w~ is {max}");
                    else
                    {
                        value = newvalue;
                    }
                }
                return value.ToString("F3");
            }
            else if (fieldType == FieldType.Vector3Type)
            {
                var value = float.Parse(currentItem);
                var min = (fieldInfo as FieldInfo<Vector3>).Min;
                var max = (fieldInfo as FieldInfo<Vector3>).Max;

                var minValueX = min.X;
                var minValueY = min.Y;
                var minValueZ = min.Z;
                var maxValueX = max.X;
                var maxValueY = max.Y;
                var maxValueZ = max.Z;

                if (itemText.EndsWith("_x"))
                {
                    if (left)
                    {
                        var newvalue = value - FloatStep;
                        if (newvalue < minValueX)
                            Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{itemText}~w~ is {minValueX}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + FloatStep;
                        if (newvalue > maxValueX)
                            Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{itemText}~w~ is {maxValueX}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
                else if (itemText.EndsWith("_y"))
                {
                    if (left)
                    {
                        var newvalue = value - FloatStep;
                        if (newvalue < minValueY)
                            Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{itemText}~w~ is {minValueY}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + FloatStep;
                        if (newvalue > maxValueY)
                            Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{itemText}~w~ is {maxValueY}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
                else if (itemText.EndsWith("_z"))
                {
                    if (left)
                    {
                        var newvalue = value - FloatStep;
                        if (newvalue < minValueZ)
                            Screen.ShowNotification($"{ScriptName}: Min value allowed for ~b~{itemText}~w~ is {minValueZ}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    else
                    {
                        var newvalue = value + FloatStep;
                        if (newvalue > maxValueZ)
                            Screen.ShowNotification($"{ScriptName}: Max value allowed for ~b~{itemText}~w~ is {maxValueZ}");
                        else
                        {
                            value = newvalue;
                        }
                    }
                    return value.ToString("F3");
                }
            }

            return currentItem;
        }

        private MenuDynamicListItem AddDynamicFloatList(Menu menu, FieldInfo<float> fieldInfo)
        {
            string name = fieldInfo.Name;
            string description = fieldInfo.Description;
            float min = fieldInfo.Min;
            float max = fieldInfo.Max;

            if (!CurrentPreset.Fields.ContainsKey(name))
                return null;

            float value = CurrentPreset.Fields[name];
            var newitem = new MenuDynamicListItem(name, value.ToString("F3"), DynamicListChangeCallback, description)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitem);

            return newitem;
        }

        private MenuDynamicListItem AddDynamicIntList(Menu menu, FieldInfo<int> fieldInfo)
        {
            string name = fieldInfo.Name;
            string description = fieldInfo.Description;
            int min = fieldInfo.Min;
            int max = fieldInfo.Max;

            if (!CurrentPreset.Fields.ContainsKey(name))
                return null;

            int value = CurrentPreset.Fields[name];
            var newitem = new MenuDynamicListItem(name, value.ToString(), DynamicListChangeCallback, description)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitem);

            return newitem;
        }

        private MenuDynamicListItem[] AddDynamicVector3List(Menu menu, FieldInfo<Vector3> fieldInfo)
        {
            string fieldName = fieldInfo.Name;

            if (!CurrentPreset.Fields.ContainsKey(fieldName))
                return null;

            string fieldDescription = fieldInfo.Description;
            Vector3 fieldMin = fieldInfo.Min;
            Vector3 fieldMax = fieldInfo.Max;

            string fieldNameX = $"{fieldName}_x";
            float valueX = CurrentPreset.Fields[fieldName].X;
            float minValueX = fieldMin.X;
            float maxValueX = fieldMax.X;
            
            var newitemX = new MenuDynamicListItem(fieldNameX, valueX.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemX);

            string fieldNameY = $"{fieldName}_y";
            float valueY = CurrentPreset.Fields[fieldName].Y;
            float minValueY = fieldMin.Y;
            float maxValueY = fieldMax.Y;

            var newitemY = new MenuDynamicListItem(fieldNameY, valueY.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemY);

            string fieldNameZ = $"{fieldName}_z";
            float valueZ = CurrentPreset.Fields[fieldName].Z;
            float minValueZ = fieldMin.Z;
            float maxValueZ = fieldMax.Z;

            var newitemZ = new MenuDynamicListItem(fieldNameZ, valueZ.ToString("F3"), DynamicListChangeCallback, fieldDescription)
            {
                ItemData = fieldInfo
            };
            menu.AddMenuItem(newitemZ);

            return new MenuDynamicListItem[3] { newitemX, newitemY, newitemZ };
        }

        private MenuItem AddLockedItem(Menu menu, BaseFieldInfo fieldInfo)
        {
            var newitem = new MenuItem(fieldInfo.Name, fieldInfo.Description)
            {
                Enabled = false,
                RightIcon = MenuItem.Icon.LOCK,
                ItemData = fieldInfo.Name,
            };

            menu.AddMenuItem(newitem);

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    Screen.ShowNotification($"{ScriptName}: The server doesn't allow to edit this field.");
                }
            };
            return newitem;
        }

        #endregion
    }
}