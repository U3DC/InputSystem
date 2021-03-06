#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ISX.HID.Editor
{
    // A window that dumps a raw HID descriptor in a tree view.
    internal class HIDDescriptorWindow : EditorWindow, ISerializationCallbackReceiver
    {
        public static void CreateOrShowExisting(HID device)
        {
            // See if we have an existing window for the device and if so pop it
            // in front.
            if (s_OpenWindows != null)
            {
                for (var i = 0; i < s_OpenWindows.Count; ++i)
                {
                    var existingWindow = s_OpenWindows[i];
                    if (existingWindow.m_DeviceId == device.id)
                    {
                        existingWindow.Show();
                        existingWindow.Focus();
                        return;
                    }
                }
            }

            // No, so create a new one.
            var window = CreateInstance<HIDDescriptorWindow>();
            window.InitializeWith(device);
            window.minSize = new Vector2(270, 200);
            window.Show();
            window.titleContent = new GUIContent("HID Descriptor");
        }

        public void Awake()
        {
            AddToList();
        }

        public void OnDestroy()
        {
            RemoveFromList();
        }

        public void OnGUI()
        {
            if (m_Device == null)
            {
                m_Device = InputSystem.TryGetDeviceById(m_DeviceId) as HID;
                if (m_Device == null)
                    return;

                InitializeWith(m_Device);
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(m_Label, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_TreeView.OnGUI(rect);
        }

        private void InitializeWith(HID device)
        {
            m_Device = device;
            m_DeviceId = device.id;

            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            m_TreeView = new HIDDescriptorTreeView(m_TreeViewState, m_Device.hidDescriptor);
            m_TreeView.SetExpanded(1, true);

            m_Label = new GUIContent(string.Format("HID Descriptor for '{0} {1}'", m_Device.description.manufacturer,
                        m_Device.description.product));
        }

        [NonSerialized] private HID m_Device;
        [NonSerialized] private HIDDescriptorTreeView m_TreeView;
        [NonSerialized] private GUIContent m_Label;

        [SerializeField] private int m_DeviceId;
        [SerializeField] private TreeViewState m_TreeViewState;

        private void AddToList()
        {
            if (s_OpenWindows == null)
                s_OpenWindows = new List<HIDDescriptorWindow>();
            if (!s_OpenWindows.Contains(this))
                s_OpenWindows.Add(this);
        }

        private void RemoveFromList()
        {
            if (s_OpenWindows != null)
                s_OpenWindows.Remove(this);
        }

        private static List<HIDDescriptorWindow> s_OpenWindows;

        private class HIDDescriptorTreeView : TreeView
        {
            private HID.HIDDeviceDescriptor m_Descriptor;

            public HIDDescriptorTreeView(TreeViewState state, HID.HIDDeviceDescriptor descriptor)
                : base(state)
            {
                m_Descriptor = descriptor;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                var id = 0;

                var root = new TreeViewItem
                {
                    id = id++,
                    depth = -1
                };

                var item = BuildDeviceItem(m_Descriptor, ref id);
                root.AddChild(item);

                return root;
            }

            private TreeViewItem BuildDeviceItem(HID.HIDDeviceDescriptor device, ref int id)
            {
                var item = new TreeViewItem
                {
                    id = id++,
                    depth = 0,
                    displayName = "Device"
                };

                AddChild(item, "Vendor ID: " + device.vendorId, ref id);
                AddChild(item, "Product ID: " + device.productId, ref id);
                AddChild(item, string.Format("Usage Page: 0x{0:X} ({1})", (uint)device.usagePage, device.usagePage), ref id);
                AddChild(item, string.Format("Usage: 0x{0:X}", device.usage), ref id);
                AddChild(item, "Input Report Size: " + device.inputReportSize, ref id);
                AddChild(item, "Output Report Size: " + device.outputReportSize, ref id);
                AddChild(item, "Feature Report Size: " + device.featureReportSize, ref id);

                // Elements.
                if (device.elements != null)
                {
                    var bitOffset = 0;
                    var currentReportType = HID.HIDReportType.Unknown;
                    var elementCount = device.elements.Length;
                    var elements = AddChild(item, elementCount + " Elements", ref id);
                    for (var i = 0; i < elementCount; ++i)
                        BuildElementItem(i, elements, device.elements[i], ref id, ref bitOffset, ref currentReportType);
                }

                ////TODO: collections

                return item;
            }

            private TreeViewItem BuildElementItem(int index, TreeViewItem parent, HID.HIDElementDescriptor element, ref int id, ref int runningBitOffset, ref HID.HIDReportType currentReportType)
            {
                var item = AddChild(parent, string.Format("Element {0} ({1})", index, element.reportType), ref id);

                AddChild(item, string.Format("Usage Page: 0x{0:X} ({1})", (uint)element.usagePage, element.usagePage), ref id);
                AddChild(item, string.Format("Usage: 0x{0:X}", element.usage), ref id);
                AddChild(item, "Report Type: " + element.reportType, ref id);
                AddChild(item, "Report ID: " + element.reportId, ref id);
                AddChild(item, "Report Size in Bits: " + element.reportSizeInBits, ref id);
                AddChild(item, "Report Count: " + element.reportCount, ref id);
                AddChild(item, "Collection Index: " + element.collectionIndex, ref id);
                AddChild(item, string.Format("Unit: {0:X}", element.unit), ref id);
                AddChild(item, string.Format("Unit Exponent: {0:X}", element.unitExponent), ref id);
                AddChild(item, "Logical Min: " + element.logicalMin, ref id);
                AddChild(item, "Logical Max: " + element.logicalMax, ref id);
                AddChild(item, "Physical Min: " + element.physicalMin, ref id);
                AddChild(item, "Physical Max: " + element.physicalMax, ref id);
                AddChild(item, "Has Null State?: " + element.hasNullState, ref id);
                AddChild(item, "Has Preferred State?: " + element.hasPreferredState, ref id);
                AddChild(item, "Is Array?: " + element.isArray, ref id);
                AddChild(item, "Is Non-Linear?: " + element.isNonLinear, ref id);
                AddChild(item, "Is Relative?: " + element.isRelative, ref id);
                AddChild(item, "Is Virtual?: " + element.isVirtual, ref id);
                AddChild(item, "Is Wrapping?: " + element.isWrapping, ref id);

                if (currentReportType != element.reportType)
                {
                    currentReportType = element.reportType;
                    runningBitOffset = 0;
                }

                AddChild(item,
                    string.Format("Inferred Offset: byte #{0}, bit #{1}", runningBitOffset / 8, runningBitOffset % 8),
                    ref id);

                runningBitOffset += element.reportSizeInBits;

                return item;
            }

            private TreeViewItem AddChild(TreeViewItem parent, string displayName, ref int id)
            {
                var item = new TreeViewItem
                {
                    id = id++,
                    depth = parent.depth + 1,
                    displayName = displayName
                };

                parent.AddChild(item);

                return item;
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            AddToList();
        }
    }
}
#endif // UNITY_EDITOR
