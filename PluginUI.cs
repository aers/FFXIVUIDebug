using FFXIVClientStructs.Component.GUI;
using FFXIVClientStructs.Component.GUI.ULD;
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

        private string FilterUnit = "";
        private bool FilterVisible = false;
        private bool highlightHovered = false;

        public unsafe void Draw()
        {
            if (!IsVisible)
                return;

            var atkStage = getAtkStageSingleton();

            ImGui.SetNextWindowSize(new Vector2(1000, 500), ImGuiCond.FirstUseEver);

            if (ImGui.Begin("UI Debug", ref visible))
            {
                ImGui.PushItemWidth(200);
                ImGui.InputText("Filter", ref FilterUnit, 30);
                ImGui.PopItemWidth();
                ImGui.Checkbox("Only visible", ref FilterVisible);
                ImGui.SameLine();
                ImGui.Checkbox("Highlight Hovered", ref highlightHovered);


                ImGui.Text($"Base - {(long)_plugin.pluginInterface.TargetModuleScanner.Module.BaseAddress:X}");
                ImGui.Text($"AtkStage - {(long)atkStage:X}");
                ImGui.Text($"RaptureAtkUnitManager - {(long)atkStage->RaptureAtkUnitManager:X}");

                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList, "Depth Layer 1");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerTwoList, "Depth Layer 2");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerThreeList, "Depth Layer 3");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerFourList, "Depth Layer 4");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerFiveList, "Depth Layer 5");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerSixList, "Depth Layer 6");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerSevenList, "Depth Layer 7");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerEightList, "Depth Layer 8");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerNineList, "Depth Layer 9");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerTenList, "Depth Layer 10");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerElevenList, "Depth Layer 11");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerTwelveList, "Depth Layer 12");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerThirteenList, "Depth Layer 13");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList, "All Loaded Units");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList, "Focused Units");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList16, "Units 16");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList17, "Units 17");
                PrintAtkUnitList(&atkStage->RaptureAtkUnitManager->AtkUnitManager.UnitList18, "Units 18");
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

                    if (FilterUnit.Length > 0 && !addonName.Contains(FilterUnit))
                        continue;
                    if (FilterVisible && !isVisible)
                        continue;

                    if (isVisible)
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

                    ImGui.Text($"ptr {(long)atkUnitBase:X} - name {addonName} - X {atkUnitBase->X} Y {atkUnitBase->Y} scale {atkUnitBase->Scale} widget count {atkUnitBase->ULDData.ObjectCount}");

                    if (isVisible)
                        ImGui.PopStyleColor();

                    if (atkUnitBase->RootNode != null)
                        PrintNode(atkUnitBase->RootNode);
                }
                ImGui.TreePop();
            }
        }

        private unsafe void PrintNode(AtkResNode* node, bool printSiblings = true, string treePrefix = "")
        {
            if (node == null)
                return;

            if ((int)node->Type < 1000)
                PrintSimpleNode(node, treePrefix);
            else
                PrintComponentNode(node, treePrefix);

            if (printSiblings)
            {
                var prevNode = node;
                while ((prevNode = prevNode->PrevSiblingNode) != null)
                    PrintNode(prevNode, false, "prev ");

                var nextNode = node;
                while ((nextNode = nextNode->NextSiblingNode) != null)
                    PrintNode(nextNode, false, "next ");
            }
        }

        private unsafe void PrintSimpleNode(AtkResNode* node, string treePrefix)
        {
            bool popped = false;
            bool isVisible = (node->Flags & 0x10) == 0x10;

            if (isVisible)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

            if (ImGui.TreeNode($"{treePrefix}{node->Type} Node (ptr = {(long)node:X})###{(long)node}"))
            {
                if (ImGui.IsItemHovered()) DrawOutline(node);
                if (isVisible)
                {
                    ImGui.PopStyleColor();
                    popped = true;
                }

                PrintResNode(node);

                if (node->ChildNode != null)
                {
                    PrintNode(node->ChildNode);
                }

                switch (node->Type)
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
                        if (imageNode->PartsList != null)
                        {
                            if (imageNode->PartId > imageNode->PartsList->PartCount)
                                ImGui.Text("part id > part count?");
                            else
                            {
                                var textureInfo = imageNode->PartsList->Parts[imageNode->PartId].ULDTexture;
                                var texType = textureInfo->AtkTexture.TextureType;

                                ImGui.Text($"texture type: {texType} part_id={imageNode->PartId} part_id_count={imageNode->PartsList->PartCount}");
                                if (texType == TextureType.Resource)
                                {
                                    var texFileNamePtr = textureInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
                                    var texString = Marshal.PtrToStringAnsi(new IntPtr(texFileNamePtr));
                                    ImGui.Text($"texture path: {texString}");
                                }
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
            else if(ImGui.IsItemHovered()) DrawOutline(node);

            if (isVisible && !popped)
                ImGui.PopStyleColor();
        }

        private unsafe void PrintComponentNode(AtkResNode* node, string treePrefix)
        {
            var compNode = (AtkComponentNode*)node;

            bool popped = false;
            bool isVisible = (node->Flags & 0x10) == 0x10;

            if (isVisible)
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));

            var componentInfo = compNode->Component->ULDData;

            var childCount = componentInfo.NodeListCount;

            var objectInfo = (ULDComponentInfo*)componentInfo.Objects;
            if (ImGui.TreeNode($"{treePrefix}{objectInfo->ComponentType} Component Node (ptr = {(long)node:X}, component ptr = {(long)compNode->Component:X}) child count = {childCount}  ###{(long)node}"))
            {
                if (ImGui.IsItemHovered()) DrawOutline(node);
                if (isVisible)
                {
                    ImGui.PopStyleColor();
                    popped = true;
                }

                PrintResNode(node);
                PrintNode(componentInfo.RootNode);

                switch (objectInfo->ComponentType)
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
            else if (ImGui.IsItemHovered()) DrawOutline(node);

            if (isVisible && !popped)
                ImGui.PopStyleColor();
        }

        private unsafe void PrintResNode(AtkResNode* node)
        {
            ImGui.Text($"NodeID: {node->NodeID}");
            ImGui.Text(
                $"X: {node->X} Y: {node->Y} " +
                $"ScaleX: {node->ScaleX} ScaleY: {node->ScaleY} " +
                $"Rotation: {node->Rotation} " +
                $"Width: {node->Width} Height: {node->Height} " +
                $"OriginX: {node->OriginX} OriginY: {node->OriginY}");
            ImGui.Text(
                $"RGBA: 0x{node->Color.R:X2}{node->Color.G:X2}{node->Color.B:X2}{node->Color.A:X2} " +
                $"AddRGB: {node->AddRed} {node->AddGreen} {node->AddBlue} " +
                $"MultiplyRGB: {node->MultiplyRed} {node->MultiplyGreen} {node->MultiplyBlue}");
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

        private unsafe Vector2 GetNodePosition(AtkResNode* node) {
            var pos = new Vector2(node->X, node->Y);
            var par = node->ParentNode;
            while (par != null) {
                pos *= new Vector2(par->ScaleX, par->ScaleY);
                pos += new Vector2(par->X, par->Y);
                par = par->ParentNode;
            }
            return pos;
        }

        private unsafe Vector2 GetNodeScale(AtkResNode* node) {
            if (node == null) return new Vector2(1, 1);
            var scale = new Vector2(node->ScaleX, node->ScaleY);
            while (node->ParentNode != null) {
                node = node->ParentNode;
                scale *= new Vector2(node->ScaleX, node->ScaleY);
            }
            return scale;
        }

        private unsafe bool GetNodeVisible(AtkResNode* node) {
            if (node == null) return false;
            while (node != null) {
                if ((node->Flags & (short)NodeFlags.Visible) != (short)NodeFlags.Visible) return false;
                node = node->ParentNode;
            }
            return true;
        }

        private unsafe void DrawOutline(AtkResNode* node) {
            if (!highlightHovered) return;
            var position = GetNodePosition(node);
            var scale = GetNodeScale(node);
            var size = new Vector2(node->Width, node->Height) * scale;
            
            var nodeVisible = GetNodeVisible(node);
            ImGui.GetForegroundDrawList().AddRect(position, position + size, nodeVisible ? 0xFF00FF00 : 0xFF0000FF);
        }

    }
}
