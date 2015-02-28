<?xml version="1.0" encoding="utf-8"?>
<DockPanel
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    LastChildFill="True"
    DataContext="{Binding Source={Model 03F534A5-4495-4223-A7EB-0FC8D477CF4B}}"
    >

    <ListView Style="{ThemeResource ContentsMenuListViewStyle}" ItemsSource="{Binding Emulators}">
        <ListView.Resources>
            <CommandBridge x:Key="Menu_Command" Command="{Binding Command}"/>
        </ListView.Resources>
    </ListView>
</DockPanel>