   M S B A M L   `   `   `  ���� a  ]PresentationFramework.Aero, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35gJclr-namespace:Microsoft.Windows.Themes;assembly=PresentationFramework.AeroMicrosoft.Windows.Themes  \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089   ScheduleStyle2  �� 
TargetType=   7System.Windows.Controls.Primitives.DataGridColumnHeader �� Property �� Value �� 
TargetType'  !System.Windows.VisualStateManager   VisualStateGroups%  System.Windows.VisualStateGroup   States   System.Windows.VisualState ��Name �� Padding �� 
StartPoint	 �� EndPoint
 � Offset � Color J� SnapsToDevicePixels  ScheduleStyle  ScheduleStyleDataGridRow)  #System.Windows.Controls.DataGridRow   
Storyboard �� 
TargetName �� TargetProperty8  2System.Windows.Media.Animation.EasingColorKeyFrame �� KeyTime �� Value?  9System.Windows.Controls.Primitives.SelectiveScrollingGrid � ColumnDefinitions � RowDefinitions?  9System.Windows.Controls.Primitives.DataGridCellsPresenter   
ItemsPanelA  ;System.Windows.Controls.Primitives.DataGridDetailsPresenter   DetailsVisibility$   SelectiveScrollingOrientation;	  5System.Windows.Controls.SelectiveScrollingOrientation 	  Vertical �� ConverterParameter&
   System.Windows.Controls.DataGrid# 
  RowDetailsScrollingConverter �� 	Converter �� RelativeSource �� AncestorType:  4System.Windows.Controls.Primitives.DataGridRowHeader7  1System.Windows.Controls.DataGridHeadersVisibility
   Row! 
  HeadersVisibilityConverter*  $System.Windows.Controls.DataGridCell g� Duration  R� KeyTime! R� Value  System.Windows.Visibility" ��Name# F�Name$ �� Stretch  ScheduleStyle2DataGrid% �� CornerRadius& ��Name' 
  SelectAllCommand( �� 
ResourceId) �� TypeInTargetAssembly
*   AllG  ASystem.Windows.Controls.Primitives.DataGridColumnHeadersPresenter+  Name,   Column- ��Name. �� CanContentScroll/ ��Name0 �� ScrollableHeight1 � Maximum2 �� Orientation  MyScrollBar3 �� ViewportHeight4 �� ViewportSize*5 �� #ComputedVerticalScrollBarVisibility6 � Value7 �� Mode8 �� ScrollableWidth9 �� ViewportWidth,: �� %ComputedHorizontalScrollBarVisibility; �� Triggers< P� Property= P� Value  bool2VisibilityConverter3   -Microsoft.Windows.Themes.DataGridHeaderBorder> J� IsMouseOver?   	IsHovered@   	IsPressedA   IsRowSelectedB   
IsSelectedC   OrientationD   SeparatorBrushE   SeparatorBrushF   SeparatorVisibilityG   SeparatorVisibilityH �� Orientation�� D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    :x,http://schemas.microsoft.com/winfx/2006/xaml     gMicrosoft_Windows_ThemesJclr-namespace:Microsoft.Windows.Themes;assembly=PresentationFramework.Aero  %�!  &	        &	 �    &	 �    '  L
    '  �    'd� S    &	 �    '
  �    &	 �    '  �    7  �� "    .���  � ��$ Center=���  � ��$ 45����  �@  SeparatorBrush
 �  ����  � ��$ Black"���  � �� �� "   .R��    CommonStates��.   
Normal��  	MouseOver��.+��� $ columnHeaderBorder��$��0,0,0,0q�$ 3,0,3,0q����� $ 0.5,0&�$	 0.5,1&�.��� $
 0Q�$ #5e5e5e��� $
 0.997Q�$ 	#FFE8E8E8��._��� #��z��#��z��# z ���� $ 0.5,0&�$	 0.5,1&�.��� $
 0.003Q�$ 	#FF6A6A6A��� $
 0.422Q�$ #f0f0f0���� "    .���  � ��$ Center=���  � ��$ 45����  �@  SeparatorBrush
 �  ����  � ��$ Black"���  � �� �� "   .R��    CommonStates��.   
Normal��  	MouseOver��.+��� $ columnHeaderBorder��$��1q�$ 3,0,3,0q����	#FF5854a5���� $ 0.5,0&�$	 0.5,1&�.��� $
 0Q�$ 	#FFD0D9EA��� $
 1Q�$ 	#FFD0D9EA��._��� #��z��#��z��# z �� "   .���  � ��
 �������  �@J�SnapsToDevicePixels . ��  �@=�ErrorTemplate Z� ��  �@ ValidationErrorTemplate �� .R��� $��2,0,0,0q�$��Center=�
���  ��$��!����  � �� �� "  .R��� $ 
DGR_Border��#��z��#��z�� . ���� $ 0.5,0&�$	 0.5,1&�.��� $
 0Q�$ #f0f0f0��� $
 1Q�$ #f0f0f0��   CommonStates��.   
Normal��  Normal_AlternatingRow��. �� .���� $ 
DGR_Border��$N H(Panel.Background).(GradientBrush.GradientStops)[0].(GradientStop.Color)�.g�  $ 0��$ 	#FFC3C7F1���� $ 
DGR_Border��$N H(Panel.Background).(GradientBrush.GradientStops)[1].(GradientStop.Color)�.g�  $ 0��$ 	#FFC3C7F1��  Normal_Selected��. �� .���� $ 
DGR_Border��$g a(Panel.Background).                         (GradientBrush.GradientStops)[0].(GradientStop.Color)�.g�  $ 0��$ 	#CC10379E���� $ 
DGR_Border��$g a(Panel.Background).                         (GradientBrush.GradientStops)[1].(GradientStop.Color)�.g�  $ 0��$ 	#CC10379E��  	MouseOver��. �� .���� $ 
DGR_Border��$g a(Panel.Background).                         (GradientBrush.GradientStops)[0].(GradientStop.Color)�.g�  $ 0��$ 	#AA5EB3EE���� $ 
DGR_Border��$g a(Panel.Background).                         (GradientBrush.GradientStops)[1].(GradientStop.Color)�.g�  $ 0��$ 	#AA5EB3EE��._�   �� $
��Auto ��� $��* � �� $��* ��� $
��Auto �.+�  $��1��#��z # z   $��1��$��1��#y�z  �� *AreRowDetailsFrozen+# Z # Z  �� " 
   $��2��$ Vertical=�y��� *HeadersVisibility+# Z # Z  �� " 
 �� "   .���  � ��
 �������  �@J�SnapsToDevicePixels . ��  �@=�ErrorTemplate Z� ��  �@ ValidationErrorTemplate �� .R��� $��2,0,0,0q�$��Center=�
���  ��$��!����  � �� �� "  .R��� $ 
DGR_Border��#��z��#��z�� . ���� $ 0.5,0&�$	 0.5,1&�.��� $
 0Q�$ 	#AA93CAF7��� $
 1Q�$ 	#AA2064A8��   CommonStates��.   
Normal��  Normal_AlternatingRow��. �� .���� $ 
DGR_Border��$N H(Panel.Background).(GradientBrush.GradientStops)[0].(GradientStop.Color)�.g�  $ 0��$ 	#FFC3C7F1���� $ 
DGR_Border��$N H(Panel.Background).(GradientBrush.GradientStops)[1].(GradientStop.Color)�.g�  $ 0��$ 	#FFC3C7F1��  Normal_Selected��. �� .���� $ 
DGR_Border��$g a(Panel.Background).                         (GradientBrush.GradientStops)[0].(GradientStop.Color)�.g�  $ 0��$ 	#CC10379E���� $ 
DGR_Border��$g a(Panel.Background).                         (GradientBrush.GradientStops)[1].(GradientStop.Color)�.g�  $ 0��$ 	#CC10379E��  	MouseOver��. �� .���� $ 
DGR_Border��$g a(Panel.Background).                         (GradientBrush.GradientStops)[0].(GradientStop.Color)�.g�  $ 0��$ 	#AA5EB3EE���� $ 
DGR_Border��$g a(Panel.Background).                         (GradientBrush.GradientStops)[1].(GradientStop.Color)�.g�  $ 0��$ 	#AA5EB3EE��._�   �� $
��Auto ��� $��* � �� $��* ��� $
��Auto �.+�  $��1��#��z # z   $��1��$��1��#y�z  �� *AreRowDetailsFrozen+# Z # Z  �� " 
   $��2��$ Vertical=�y��� *HeadersVisibility+# Z # Z  �� " 
 �� "   .���  � �� �� "  .R��� $ border��
������ 
������ $��1q� .    FocusStates��.   	Unfocused��  Focused��  CurrentStates��.   Regular��  Current��. �� .���� $ border��$2 ,(Border.BorderBrush).(SolidColorBrush.Color)�.g�  $ 0��$
 Gray��._��� # z �� "  d�.���  � ��$ 3q���  � �� �� " d�.R��    CommonStates��.   
Normal��  	MouseOver��. �� .���� $ DefaultPath��$) #(Path.Fill).(SolidColorBrush.Color)�.g��� $ 0:0:0.2��$ #AA0000��  Pressed��  Disabled��  CheckStates��.   Checked��. �� .��T� $ 0D�$ DefaultPath��$ 
Visibility�..�i� $  0��!  	Collapsed=�T� $ 0D�$ CheckedPath��$ 
Visibility�..�i� $  0��!  Visible=��� $ CheckedPath��$) #(Path.Fill).(SolidColorBrush.Color)�.g��� $ 0:0:0.2��$ 	#CCAA0000��  	Unchecked��.+��� $" contentPresenter��#��z��#��z��#��z��#��z��#��z��F� $# DefaultPath��$	��Top=�"����        р�� ��j �    ��� 	
�������$
$ Fill=�F� $# CheckedPath��$	��Top=�"����        р��     ���j ��� 	
�������$
$ Fill=�$y�	Collapsed=��� "  
 .���  � ��# � ����   �@
 RowDetailsVisibilityMode$ VisibleWhenSelected=���  � �� . ��  �@��PanningMode$
 Both=���  �@��IsFlicksEnabled .  ��  � �� �� " 
 .R��� #��z��#��z��$��0q�$% 5��# z�� . ._��� $& DG_ScrollViewer��|�.  ���� " ��.R��  �� $
��Auto ��� $��* ��� $
��Auto � �� $
��Auto ��� $��* ��� $
��Auto �.+��� #��Z' |�.  ���� *CellsPanelHorizontalOffset+ �� " 
 ��C� *�� $"( DataGridSelectAllButtonStyle��") 
 +y��� *HeadersVisibility+# Z* # Z  �� " 
   $!+ PART_ColumnHeadersPresenter��$��1��y��� *HeadersVisibility+# Z, # Z  �� " 
 �� $!- PART_ScrollContentPresenter��$��1��$��2��#. z���� $/ PART_VerticalScrollBar��$��1��$��2��#1 z0 $2 Vertical=�#���  #4 z3 #y�z5 6 �� *VerticalOffset+$7 OneWay=� �� *TemplatedParent+� $��2��$��1�� �� ���� **(NonFrozenColumnsViewportHorizontalOffset+ �� " 
 �� $��* �.+��� $/ PART_HorizontalScrollBar��$��1��#1 z8 $2 
Horizontal=�#4 z9 #y�z: 6 �� *HorizontalOffset+$7 OneWay=� �� *TemplatedParent+.���� # z ; P� < �@��
IsGrouping= . . ���  � �� . �� "  
 .���  � ��# � ����   �@
 RowDetailsVisibilityMode$ VisibleWhenSelected=���  � �� . ��  �@��PanningMode$
 Both=���  �@��IsFlicksEnabled .  ��  � �� �� " 
 .R��� #��z��#��z��$��2q�$% 5��# z�� . ._��� $& DG_ScrollViewer��|�.  ���� " ��.R��  �� $
��Auto ��� $��* ��� $
��Auto � �� $
��Auto ��� $��* ��� $
��Auto �.+��� #��Z' |�.  ���� *CellsPanelHorizontalOffset+ �� " 
 ��C� *�� $"( DataGridSelectAllButtonStyle��") 
 +y��� *HeadersVisibility+# Z* # Z  �� " 
   $!+ PART_ColumnHeadersPresenter��$��1��y��� *HeadersVisibility+# Z, # Z  �� " 
 �� $!- PART_ScrollContentPresenter��$��1��$��2��#. z���� $/ PART_VerticalScrollBar��$��1��$��2��#1 z0 $2 Vertical=�#���  #4 z3 #y�z5 6 �� *VerticalOffset+$7 OneWay=� �� *TemplatedParent+� $��2��$��1�� �� ���� **(NonFrozenColumnsViewportHorizontalOffset+ �� " 
 �� $��* �.+��� $/ PART_HorizontalScrollBar��$��1��#1 z8 $2 
Horizontal=�#4 z9 #y�z: 6 �� *HorizontalOffset+$7 OneWay=� �� *TemplatedParent+.���� # z ; P� < �@��
IsGrouping= . . ���  � �� . �� �� "   .���  � ��$ 1����  � �� �� "  .R�� .+�  #��z��#��z��#��z��#? z> #@ z��#B zA $C 
Horizontal=�# z��#E zD #G zF ._��� $H 
Horizontal=�.+��� $��Center=�# z ��  .  ���� *ValidationErrorTemplate+ �� "  y��� *(Validation.HasError)+8    �� "  