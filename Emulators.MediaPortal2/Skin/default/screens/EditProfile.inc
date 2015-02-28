<?xml version="1.0" encoding="utf-8"?>
<Grid
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 03F534A5-4495-4223-A7EB-0FC8D477CF4B}}"
    >
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto" />
    <ColumnDefinition Width="*" />
    <ColumnDefinition Width="Auto" />
  </Grid.ColumnDefinitions>
  <Grid.RowDefinitions>
    <RowDefinition />
    <RowDefinition />
    <RowDefinition Height="20" />
    <RowDefinition />
    <RowDefinition />
    <RowDefinition />
  </Grid.RowDefinitions>

  <Label Content="[Emulators.EmulatorPath]" Grid.Column="0" Grid.Row="0" HorizontalAlignment="Left" Margin="10,10,0,0" VerticalAlignment="Center" Color="{ThemeResource TextColor}"/>
  <TextBox Grid.Column="1" Grid.Row="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Text="{Binding Path=EmulatorPath, Mode=TwoWay}"
      VerticalContentAlignment="Center"  Margin="10,10,0,0" />
  <Button Grid.Column="2" Grid.Row="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="5,10,20,0" Style="{ThemeResource ButtonWideStyle}"
      Content="..." Command="{Command ChooseEmulatorPath}"/>

  <Label Content="[Emulators.Arguments]" Grid.Column="0" Grid.Row="1" HorizontalAlignment="Left" Margin="10,10,0,0" VerticalAlignment="Center" Color="{ThemeResource TextColor}"/>
  <TextBox Text="{Binding Path=Arguments, Mode=TwoWay}" Grid.Column="1" Grid.Row="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" VerticalContentAlignment="Center"  Margin="10,10,0,0" />

  <CheckBox VerticalAlignment="Center" Content="[Emulators.UseQuotes]" Grid.Column="1" Grid.Row="3" Margin="10,10,0,0"
            IsChecked="{Binding Path=UseQuotes, Mode=TwoWay}"/>
  <CheckBox VerticalAlignment="Center" Content="[Emulators.EscToExit]" Grid.Column="1" Grid.Row="4" Margin="10,10,0,0"
            IsChecked="{Binding Path=EscToExit, Mode=TwoWay}"/>

  <CheckBox VerticalAlignment="Center" Content="[Emulators.WarnNoControllers]" Grid.Column="1" Grid.Row="5" Margin="10,10,0,0"
            IsChecked="{Binding Path=WarnNoControllers, Mode=TwoWay}"/>
</Grid>