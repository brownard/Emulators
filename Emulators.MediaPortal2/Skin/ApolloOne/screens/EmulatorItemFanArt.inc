<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  x:Name="EmulatorItemDetails"
  Width="680" Height="560"
  Margin="35,10,0,14"
  OpacityMask="{ThemeResource HomeTileOpacityBrush}"
  DataContext="{Binding Source={StaticResource EmulatorsModel}, Path=FocusedItem}">
  <!-- Rounded corners are applied by OpacityMask -->

  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
    <RowDefinition Height="60"/>
  </Grid.RowDefinitions>

  <!-- Image -->
  <Image Grid.Column="0" Grid.RowSpan="2" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="UniformToFill"
      Source="{Binding Fanart}">
  </Image>

  <!-- Darkened band -->
  <Rectangle Grid.Row="1" Grid.Column="0" Fill="#FF000000" Opacity="0.749"
             HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,0,0,0"
             IsVisible="{Binding Path=Fanart, Converter={StaticResource ReferenceToBoolConverter}}"/>

  <!-- Item label -->
  <Label Grid.Row="1" Grid.Column="0" x:Name="ItemLabel" HorizontalAlignment="Center" VerticalAlignment="Center" Content="{Binding Name}"
         Color="{ThemeResource TextColor}"/>
</Grid>
