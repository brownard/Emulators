<?xml version="1.0" encoding="utf-8"?>
<DockPanel
      xmlns="www.team-mediaportal.com/2008/mpf/directx"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      DataContext="{Binding Source={Model 03F534A5-4495-4223-A7EB-0FC8D477CF4B}}"
      LastChildFill="True"
    >

  <Grid DockPanel.Dock="Top">
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*" />
      <ColumnDefinition Width="*" />
      <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
      <RowDefinition />
    </Grid.RowDefinitions>
    <RadioButton GroupName="emuProfileTabs" Name="emuTabGeneral" Grid.Column="0" IsChecked="True" Content="[Emulators.General]" Style="{ThemeResource EmulatorsTabButtonStyle}"/>
    <RadioButton GroupName="emuProfileTabs" Name="emuTabAdvanced" Grid.Column="1" Content="[Emulators.Advanced]" Style="{ThemeResource EmulatorsTabButtonStyle}"/>
    <RadioButton GroupName="emuProfileTabs" Name="emuTabGoodmerge" Grid.Column="2" Content="[Emulators.Goodmerge]" Style="{ThemeResource EmulatorsTabButtonStyle}"/>
  </Grid>

  <!--General Settings-->
  
  <Grid IsVisible="{Binding ElementName=emuTabGeneral, Path=IsChecked}">
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

  <!--Advanced Settings-->

  <Grid IsVisible="{Binding ElementName=emuTabAdvanced, Path=IsChecked}">
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

  <!--Goodmerge Settings-->

  <Grid IsVisible="{Binding ElementName=emuTabGoodmerge, Path=IsChecked}">
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
  
</DockPanel>