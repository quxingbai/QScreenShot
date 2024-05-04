 //在工具类中有写一个方便调用的Show方法 返回一个window对象
 <br/>
  ScreenUtils.Show((img) =>
            {
                BitmapImage bmpImage = img.Image.Value;
                System.Drawing.Bitmap bmp = img.BitmapSource;
            });
