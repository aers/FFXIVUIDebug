using FFXIVClientStructs.Component.GUI;
using FFXIVClientStructs.Client.System.Resource.Handle;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace FFXIVUIDebug
{
    public class PluginUI : IDisposable
    {
        private bool disposedValue;

        private unsafe delegate AtkStage* GetAtkStageSingleton();
        private GetAtkStageSingleton getAtkStageSingleton;

        private bool visible = false;
        public bool IsVisible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private Plugin _plugin;

        public PluginUI(Plugin p)
        {
            _plugin = p;
        }

        public void Init()
        {
            var scanner = _plugin.pluginInterface.TargetModuleScanner;

            var getSingletonAddr = scanner.ScanText("E8 ?? ?? ?? ?? 41 B8 01 00 00 00 48 8D 15 ?? ?? ?? ?? 48 8B 48 20 E8 ?? ?? ?? ?? 48 8B CF");

            this.getAtkStageSingleton = Marshal.GetDelegateForFunctionPointer<GetAtkStageSingleton>(getSingletonAddr);
        }

        public unsafe void Draw()
        {
            if (!IsVisible)
                return;

            var atkStage = getAtkStageSingleton();

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(1000, 500), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("UI Debug", ref visible))
            {
                ImGui.Text($"AtkStage - {(long)atkStage:X}");
                ImGui.Text($"RaptureAtkUnitManager - {(long)atkStage->RaptureAtkUnitManager:X}");

                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList1, "Unit List 1");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList2, "Unit List 2");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList3, "Unit List 3");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList4, "Unit List 4");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList5, "Unit List 5");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList6, "Unit List 6");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList7, "Unit List 7");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList8, "Unit List 8");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList9, "Unit List 9");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList10, "Unit List 10");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList11, "Unit List 11");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList12, "Unit List 12");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList13, "Unit List 13");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.MainUIAddonUnitList, "Main UI Addons");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList15, "Unit List 15");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList16, "Unit List 16");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList17, "Unit List 17");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList18, "Unit List 18");
            }
        }

        private unsafe void PrintAtkUnitList(AtkUnitList* list, string name)
        {
            ImGui.Separator();

            var atkUnitBaseArray = &(list->AtkUnitEntries);

            if (ImGui.TreeNode($"AtkUnitList (ptr = {(long)list:X}) - {name} - count - {list->Count}###{(long)list}"))
            {
                for (int i = 0; i < list->Count; i++)
                {
                    var atkUnitBase = atkUnitBaseArray[i];
                    bool isVisible = (atkUnitBase->Flags & 0x20) == 0x20;
                    string addonName = Marshal.PtrToStringAnsi(new IntPtr(atkUnitBase->Name));

                    if (isVisible)
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

                    ImGui.Text($"ptr {(long)atkUnitBase:X} - name {addonName} - X {atkUnitBase->X} Y {atkUnitBase->Y} scale {atkUnitBase->Scale} widget count {atkUnitBase->AddonData.WidgetCount}");

                    if (isVisible)
                        ImGui.PopStyleColor();

                    var widgets = atkUnitBase->AddonData.Widgets;
                    if (widgets != null)
                    {
                        if (ImGui.TreeNode($"child nodes tree - root node {(long)atkUnitBase->RootNode:X} - widget data {(long)widgets:X} - count {widgets->NodeCount}###{(long)widgets}"))
                        {
                            PrintNode(atkUnitBase->RootNode);

                            ImGui.TreePop();
                        }
                    }
                }
                ImGui.TreePop();
            }
        }

        private unsafe void PrintNode(AtkResNode * node)
        {
            if (node->Type < 1000)
                PrintSimpleNode(node);
            else
                PrintComponentNode(node);
        }

        private unsafe void PrintSimpleNode(AtkResNode * node)
        {
            var type = (NodeType)node->Type;

            bool popped = false;
            bool isVisible = (node->Flags & 0x10) == 0x10;

            if (isVisible)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

            if (ImGui.TreeNode($"{type} Node (ptr = {(long)node:X})###{(long)node}"))
            {
                if (isVisible)
                {
                    ImGui.PopStyleColor();
                    popped = true;
                }

                PrintResNode(node);

                if (node->ChildNode != null)
                {
                    if (ImGui.TreeNode($"children###{(long)node}children"))
                    {
                        PrintNode(node->ChildNode);
                        ImGui.TreePop();
                    }
                }

                switch (type)
                {
                    case NodeType.Text:
                        var textNode = (AtkTextNode*)node;
                        ImGui.Text($"text: {Marshal.PtrToStringAnsi(new IntPtr(textNode->NodeText.StringPtr))}");
                        break;
                    case NodeType.Counter:
                        var counterNode = (AtkCounterNode*)node;
                        ImGui.Text($"text: {Marshal.PtrToStringAnsi(new IntPtr(counterNode->NodeText.StringPtr))}");
                        break;
                    case NodeType.Image:
                        var imageNode = (AtkImageNode*)node;
                        if (imageNode->TPInfo != null)
                        {
                            if (imageNode->PartId > imageNode->TPInfo->PartCount)
                                ImGui.Text("part id > part count?");
                            else
                            {
                                var textureInfo = imageNode->TPInfo->Parts[imageNode->PartId].TextureInfo;
                                var texString = Marshal.PtrToStringAnsi(new IntPtr(textureInfo->AtkTexture.TextureInfo->TexFileResourceHandle->ResourceHandle.FileName));
                                ImGui.Text($"texture path: {texString}");
                            }

                        }
                        else
                        {
                            ImGui.Text($"no texture loaded");
                        }
                        break;
                }

                ImGui.TreePop();
            }

            if (isVisible && !popped)
                ImGui.PopStyleColor();

            if (node->NextSiblingNode != null)
                PrintNode(node->NextSiblingNode);
        }

        private unsafe void PrintComponentNode(AtkResNode * node)
        {
            var compNode = (AtkComponentNode*)node;

            bool popped = false;
            bool isVisible = (node->Flags & 0x10) == 0x10;

            if (isVisible)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

            var componentInfo = compNode->Component->ComponentInfo;

            var childCount = componentInfo != null ? componentInfo->NodeCount : 0;

            if (ImGui.TreeNode($"{(ComponentType)componentInfo->ComponentType} Component Node (ptr = {(long)node:X}, component ptr = {(long)compNode->Component:X}) child count = {childCount}###{(long)node}"))
            {
                if (isVisible)
                {
                    ImGui.PopStyleColor();
                    popped = true;
                }

                PrintResNode(node);

                if (componentInfo != null)
                {
                    if (ImGui.TreeNode($"child nodes tree - root node {(long)compNode->Component->RootNode:X} - node list {(long)componentInfo:X} - count {componentInfo->NodeCount}###{(long)componentInfo}"))
                    {
                        PrintNode(compNode->Component->RootNode);

                        ImGui.TreePop();
                    }
                }
                switch ((ComponentType)componentInfo->ComponentType)
                {
                    case ComponentType.TextInput:
                        var textInputComponent = (AtkComponentTextInput*)compNode->Component;
                        ImGui.Text($"InputBase Text1: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->AtkComponentInputBase.UnkText1.StringPtr))}");
                        ImGui.Text($"InputBase Text2: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->AtkComponentInputBase.UnkText2.StringPtr))}");
                        ImGui.Text($"Text1: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText1.StringPtr))}");
                        ImGui.Text($"Text2: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText2.StringPtr))}");
                        ImGui.Text($"Text3: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText3.StringPtr))}");
                        ImGui.Text($"Text4: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText4.StringPtr))}");
                        ImGui.Text($"Text5: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText5.StringPtr))}");
                        break;
                }

                ImGui.TreePop();
            }

            if (isVisible && !popped)
                ImGui.PopStyleColor();

            if (node->NextSiblingNode != null)
                PrintNode(node->NextSiblingNode);
        }

        private unsafe void PrintResNode(AtkResNode * node)
        {
            ImGui.Text($"X: {node->X} Y: {node->Y} ScaleX: {node->ScaleX} ScaleY: {node->ScaleY} Rotation: {node->Rotation} Alpha: {node->Alpha}");
            ImGui.Text($"Width: {node->Width} Height: {node->Height} OriginX: {node->OriginX} OriginY: {node->OriginY}");
            ImGui.Text($"AddRed: {node->AddRed} AddGreen: {node->AddGreen} AddBlue: {node->AddBlue} MultiplyRed: {node->MultiplyRed} MultiplyGreen: {node->MultiplyGreen} MultiplyBlue: {node->MultiplyBlue}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
