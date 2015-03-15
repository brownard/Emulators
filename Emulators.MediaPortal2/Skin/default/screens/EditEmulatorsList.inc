<?xml version="1.0" encoding="utf-8"?>
<DockPanel
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    LastChildFill="True"
    DataContext="{Binding Source={Model 03F534A5-4495-4223-A7EB-0FC8D477CF4B}}"
    >

    <ListView Style="{ThemeResource emuContentsMenuListViewStyle}" ItemsSource="{Binding Emulators}">
        <ListView.Resources>
            <CommandBridge x:Key="Menu_Command" Command="{Binding Command}"/>
            <CommandBridge x:Key="Context_Command" Command="{Binding ContextCommand}"/>
        </ListView.Resources>
    </ListView>
</DockPanel>