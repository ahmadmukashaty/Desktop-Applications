   M S B A M L   `   `   `  ����  PlayoutSystem%    PlayoutSystem.Views.DelayDialog   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   X TXceed.Wpf.Toolkit, Version=2.9.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit  5   	     � Title$  
Edit Delay��5   	   $	��180��6   $	��350��6)    � WindowStartupLocation$ CenterScreen=�65   .��5      � .+�5   
   "  Xceed.Wpf.Toolkit.TimePicker  -   5          Name$ channel_DelayTime��5      $��4��65   $
��Left=�6B   $��10��6]   $��5��6h   $	��Top=�6s   &   Xceed.Wpf.Toolkit.DateTimeUpDown   Format$ Custom=�6�      FormatString$ HH:mm:ss��6�   $��0,0,0,50q�6�   $��24(�6�   ,  &Xceed.Wpf.Toolkit.Primitives.InputBase   TextAlignment$ Center=�6�   $y�Hidden=�6�   �� 5   
   -   5         ��Name$ 
secondTime��5      $��50��6&   $��30��61   $��
150,0,0,20q�6=   6Q   �� 5	   
   -   5        $ 
minuteTime��5	      $��50��6&   $��30��61   $��0,0,0,20q�6=   6O   �� 5
   
   -   5        $ hourTime��5
      $��50��6$   $��30��6/   $��
0,0,150,20q�6;   6O   �� 5   
   $��Hours��6   $��
0,0,150,90q�6    $��50��64   $��30��6?   �� 5   
   $��Minutes��6   $��	10,0,0,90q�6"   $��70��65   $��30��6@   �� 5   
   $��Seconds��6   $��
190,0,0,90q�6"   $	��100��66   $��30��6B   �� 5   
   -   5        $��OK��5      $��111,95,117,20q�65   5      5      