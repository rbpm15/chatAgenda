using System;
using System.Drawing;
using System.IO;

class IconGenerator
{
    static void Main()
    {
        // Crear un icono simple para ChatAgenda (fondo azul con "C")
        string outputPath = @"Client.WPF\app.ico";

        // Crear bitmap de 256x256
        Bitmap bitmap = new Bitmap(256, 256);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            // Fondo degradado azul
            using (LinearGradientBrush brush = new LinearGradientBrush(
                new Point(0, 0), new Point(256, 256),
                Color.FromArgb(41, 128, 185),      // Azul claro
                Color.FromArgb(52, 73, 94)))        // Azul oscuro
            {
                g.FillRectangle(brush, 0, 0, 256, 256);
            }

            // Dibujar "C" en blanco
            using (Font font = new Font("Arial", 140, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString("C", font, textBrush, 128, 128, format);
            }

            // Dibujar borde
            using (Pen pen = new Pen(Color.White, 3))
            {
                g.DrawRectangle(pen, 2, 2, 252, 252);
            }
        }

        // Convertir a icono
        Icon icon = Icon.FromHandle(bitmap.GetHicon());
        using (FileStream fs = new FileStream(outputPath, FileMode.Create))
        {
            icon.Save(fs);
        }

        bitmap.Dispose();
        Console.WriteLine($"Icono creado: {outputPath}");
    }
}
