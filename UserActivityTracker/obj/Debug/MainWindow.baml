   M S B A M L   `   `   `  ����N  JUserActivityTracker, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:UserActivityTrackerUserActivityTracker  5   	   $    UserActivityTracker.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   3local!clr-namespace:UserActivityTracker     5   	     � Title$  Barcode Scan��5   	   $	��250��6   $	��400��6+   .��5	      � .+�5
   
   �� $X��RИзберете файл съдържащ баркод за валидиране:��6   $
��Left=�6G   $��	25,25,0,0q�6b   $	��Top=�6u   $��14(�6�   �� 5   
   -   5         ��Name$ txtFilePath��5      $
��Left=�6'   $��21��6B   $��	25,76,0,0q�6N   $	��Top=�6a   $	��225��6y   �� 5   
   -   5        $��Избор файл��5      $
��Left=�6&   $��
270,75,0,0q�6A   $	��Top=�6U   $	��100��6m   6y   �� 5   
   -   5        $��Сканиране��5      $
��Left=�6r   $��270,125,0,0q�6�   $	��Top=�6�   $	��100��6�   {�6%   ��  �� ElementName$ txtFilePath�� �� Path$ Text.Length� �� Mode$ OneWay=�6�   �� 5   
   $,��&Прочетени баркодове:��6   $
��Left=�6/   $��
25,100,0,0q�6J   $	��Top=�6^   �� 5   
   -   5         ��Name$ lboxBarcodes��5      $
��Left=�6(   $��74��6C   $��
25,125,0,0q�6O   $	��Top=�6c   $	��225��6{   �� 5   
   -   5        $��Валидиране��5      $
��Left=�6t   $��270,176,0,0q�6�   $	��Top=�6�   $	��100��6�   {�6&   �� $ lboxBarcodes��$ Items.Count�$ OneWay=�6�   5      5      