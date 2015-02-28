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
    <RowDefinition Height="20" />
    <RowDefinition />
    <RowDefinition />
    <RowDefinition />
  </Grid.RowDefinitions>

  <Label Content="[Emulators.Name]" Grid.Column="0" Grid.Row="0" HorizontalAlignment="Left" Margin="10,10,0,0" VerticalAlignment="Center" Color="{ThemeResource TextColor}"/>
  <TextBox Grid.Column="1" Grid.Row="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Text="{Binding Path=Name, Mode=TwoWay}"
      VerticalContentAlignment="Center"  Margin="10,10,0,0" />

  <Label x:Name="PlatformLabel" Grid.Row="1" Grid.Column="0" Color="{ThemeResource TextColor}"
      Content="[Emulators.Platform]" VerticalAlignment="Center" Margin="10,10,0,0"/>
  <Button x:Name="Platform" Height="52" Grid.Row="1" Grid.Column="1" Style="{ThemeResource ButtonWideStyle}"
      Content="{Binding Platform}" Command="{Command ChoosePlatform}" Margin="10,10,0,0"/>

  <Label x:Name="RomDirectoryLabel" Grid.Row="3" Grid.Column="0" Color="{ThemeResource TextColor}"
      Content="[Emulators.RomDirectory]" VerticalAlignment="Center" Margin="10,10,0,0"/>
  <Button x:Name="RomDirectory" Height="52" Grid.Row="3" Grid.Column="1" Style="{ThemeResource ButtonWideStyle}"
      Content="{Binding RomDirectory}" Command="{Command ChooseRomDirectory}" Margin="10,10,0,0"/>

  <Label x:Name="FiltersLabel" Grid.Row="4" Grid.Column="0" Color="{ThemeResource TextColor}"
      Content="[Emulators.Filters]" Margin="10,10,0,0"/>
  <TextBox x:Name="Filters" Grid.Row="4" Grid.Column="1"
      Text="{Binding Path=Filters, Mode=TwoWay}" Margin="10,10,0,0"/>
  
</Grid>