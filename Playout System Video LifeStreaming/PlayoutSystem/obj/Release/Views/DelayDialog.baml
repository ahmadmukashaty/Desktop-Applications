   M S B A M L   `   `   `  ����   PlayoutSystem%    PlayoutSystem.Views.DelayDialog   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     X TXceed.Wpf.Toolkit, Version=2.9.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit    � Title$  
Edit Delay��$	��180��$	��350�� � WindowStartupLocation$ CenterScreen=�.��� .+�"  Xceed.Wpf.Toolkit.TimePicker  -     Name$ channel_DelayTime��$��4��$
��Left=�$��10��$��5��$	��Top=�&   Xceed.Wpf.Toolkit.DateTimeUpDown   Format$ Custom=�   FormatString$ HH:mm:ss��$��0,0,0,50q�$��24(�,  &Xceed.Wpf.Toolkit.Primitives.InputBase   TextAlignment$ Center=�$y�Hidden=��� -    ��Name$ 
secondTime��$��50��$��30��$��
150,0,0,20q��� -   $ 
minuteTime��$��50��$��30��$��0,0,0,20q��� -   $ hourTime��$��50��$��30��$��
0,0,150,20q��� $��Hours��$��
0,0,150,90q�$��50��$��30���� $��Minutes��$��	10,0,0,90q�$��70��$��30���� $��Seconds��$��
190,0,0,90q�$	��100��$��30���� -   $��OK��$��111,95,117,20q�