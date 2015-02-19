<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  x:Name="EmulatorItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.RowDefinitions>
    <RowDefinition Height="210"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Control x:Name="ItemDetails">    
    <Control.TemplateControl>
      <Include Source="screens\EmulatorItemDetails.inc"/>
    </Control.TemplateControl>
  </Control>
</Grid>
