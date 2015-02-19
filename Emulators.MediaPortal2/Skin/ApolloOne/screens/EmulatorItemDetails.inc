<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  HorizontalAlignment="Stretch"
  DataContext="{Binding Source={StaticResource EmulatorsModel}, Path=FocusedItem}">
  
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="Auto"/>
  </Grid.ColumnDefinitions>

  <Label x:Name="EmulatorItemName" Wrap="True" Grid.Column="0"
      HorizontalAlignment="Stretch"
      Content="{Binding Name}"
      Color="{ThemeResource TextColor}"
      FontFamily="DefaultBold"/>

  <Label x:Name="EmulatorItemDescription" Wrap="True" Grid.Row="1" Grid.ColumnSpan="3"
        Scroll="Auto" ScrollSpeed="5" ScrollDelay="5"
        HorizontalAlignment="Stretch"
        Content="{Binding Description}"
        Color="{ThemeResource TextColor}"/>

</Grid>
