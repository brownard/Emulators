<?xml version="1.0" encoding="utf-8"?>
<Grid
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 03F534A5-4495-4223-A7EB-0FC8D477CF4B}}"
    >
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto" />
    <ColumnDefinition Width="*" />
  </Grid.ColumnDefinitions>
  <Grid.RowDefinitions>
    <RowDefinition />
    <RowDefinition />
    <RowDefinition />
  </Grid.RowDefinitions>

  <CheckBox Grid.Column="1" Grid.Row="0" VerticalAlignment="Center" Content="[Emulators.EnableGoodmerge]" Margin="10,10,0,0"
    IsChecked="{Binding Path=EnableGoodmerge, Mode=TwoWay}"/>
  <Label Grid.Column="0" Grid.Row="1" VerticalAlignment="Center" Color="{ThemeResource TextColor}" Margin="10,10,0,0"
    Content="[Emulators.GoodmergeTags]"/>
  <TextBox Grid.Column="1" Grid.Row="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" VerticalContentAlignment="Center" Margin="10,10,0,0"
    Text="{Binding Path=GoodmergeTags, Mode=TwoWay}"/>

</Grid>