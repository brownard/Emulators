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
    <RowDefinition />
    <RowDefinition />
    <RowDefinition />
    <RowDefinition />
  </Grid.RowDefinitions>

  <Label Content="[Emulators.WorkingDirectory]" Grid.Column="0" Grid.Row="0" HorizontalAlignment="Left" Margin="10,10,0,0" VerticalAlignment="Center" Color="{ThemeResource TextColor}"/>
  <TextBox Grid.Column="1" Grid.Row="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Text="{Binding Path=WorkingDirectory, Mode=TwoWay}"
      VerticalContentAlignment="Center"  Margin="10,10,0,0" />
  <Button Grid.Column="2" Grid.Row="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="5,10,20,0" Style="{ThemeResource ButtonWideStyle}"
      Content="..." Command="{Command ChooseWorkingDirectory}"/>

  <Label Content="[Emulators.LaunchedProcess]" Grid.Column="0" Grid.Row="1" HorizontalAlignment="Left" Margin="10,10,0,0" VerticalAlignment="Center" Color="{ThemeResource TextColor}"/>
  <TextBox Text="{Binding Path=LaunchedFile, Mode=TwoWay}" Grid.Column="1" Grid.Row="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" VerticalContentAlignment="Center" Margin="10,10,0,0"/>

  <Label Content="[Emulators.PreCommand]" Grid.Column="0" Grid.Row="2" HorizontalAlignment="Left" Margin="10,10,0,0" VerticalAlignment="Center" Color="{ThemeResource TextColor}"/>
  <TextBox Text="{Binding Path=PreCommand, Mode=TwoWay}" Grid.Column="1" Grid.Row="2" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" VerticalContentAlignment="Center" Margin="10,10,0,0"/>

  <StackPanel Orientation="Horizontal" Grid.Column="1" Grid.Row="3" >
    <CheckBox VerticalAlignment="Center" Content="[Emulators.WaitForExit]" Margin="10,5,0,0"
              IsChecked="{Binding Path=PreCommandWaitForExit, Mode=TwoWay}"/>
    <CheckBox VerticalAlignment="Center" Content="[Emulators.ShowWindow]" Margin="20,5,0,0"
              IsChecked="{Binding Path=PreCommandShowWindow, Mode=TwoWay}"/>
  </StackPanel>

  <Label Content="[Emulators.PostCommand]" Grid.Column="0" Grid.Row="4" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="10,10,0,0" Color="{ThemeResource TextColor}"/>
  <TextBox Text="{Binding Path=PostCommand, Mode=TwoWay}" Grid.Column="1" Grid.Row="4" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" VerticalContentAlignment="Center" Margin="10,10,0,0"/>

  <StackPanel Orientation="Horizontal" Grid.Column="1" Grid.Row="5" >
    <CheckBox VerticalAlignment="Stretch" Content="[Emulators.WaitForExit]" Margin="10,5,0,0"
              IsChecked="{Binding Path=PostCommandWaitForExit, Mode=TwoWay}"/>
    <CheckBox VerticalAlignment="Stretch" Content="[Emulators.ShowWindow]" Margin="20,5,0,0"
              IsChecked="{Binding Path=PostCommandShowWindow, Mode=TwoWay}"/>
  </StackPanel>
</Grid>