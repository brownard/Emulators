<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  Grid.Column="2" Grid.Row="0" x:Name="GameItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch" 
  DataContext="{Binding Source={StaticResource EmulatorsModel},Path=FocusedGame}">

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="380"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <StackPanel Grid.Column="1" Margin="10" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="180"/>
        <ColumnDefinition Width="*"/>
      </Grid.ColumnDefinitions>

      <Label x:Name="TitleLabel" Grid.Row="0" Grid.Column="0"
          Content="Title" Color="{ThemeResource EmphasisTextColor}"/>
      <Label x:Name="Title" Grid.Row="0" Grid.Column="1"
          Content="{Binding Game.Title}" Color="{ThemeResource TextColor}"/>

      <Label x:Name="YearLabel" Grid.Row="1" Grid.Column="0"
          Content="Year" Color="{ThemeResource EmphasisTextColor}"/>
      <Label x:Name="Year" Grid.Row="1" Grid.Column="1"
          Content="{Binding Game.Year}" Color="{ThemeResource TextColor}"/>

      <Label x:Name="GenreLabel" Grid.Row="2" Grid.Column="0"
          Content="Genre" Color="{ThemeResource EmphasisTextColor}"/>
      <Label x:Name="Genre" Grid.Row="2" Grid.Column="1"
          Content="{Binding Game.Genre}" Color="{ThemeResource TextColor}"/>

    </Grid>

    <Label x:Name="Description" Wrap="True" Scroll="Auto" ScrollSpeed="5" Margin="0,10,0,0" Height="320"
        HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
        Content="{Binding Game.Description}"
        Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
  </StackPanel>

  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Uniform" Margin="5"
      Width="380" Height="560" Source="{Binding FrontCover}">
  </Image>
</Grid>
