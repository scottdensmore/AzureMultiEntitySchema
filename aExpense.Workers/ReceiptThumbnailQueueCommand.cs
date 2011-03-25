namespace AExpense.Workers
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using AExpense.Data;
    using AExpense.Data.Messages;
    using AExpense.Data.Process;

    public class ReceiptThumbnailQueueCommand : IQueueCommand<NewReceiptMessage>
    {
        private const int PhotoSize = 330;
        private const int ThumbnailSize = 65;
        private readonly ExpenseRepository expenseRepository;
        private readonly ExpenseReceiptStorage receiptStorage;

        public ReceiptThumbnailQueueCommand()
        {
            this.receiptStorage = new ExpenseReceiptStorage();
            this.expenseRepository = new ExpenseRepository();
        }

        public void Run(NewReceiptMessage message)
        {
            var expenseItemId = message.ExpenseItemId;
            var imageName = expenseItemId + ".jpg";

            byte[] originalPhoto = this.receiptStorage.GetReceipt(expenseItemId);

            if (originalPhoto != null && originalPhoto.Length > 0)
            {
                var thumb = ResizeImage(originalPhoto, ThumbnailSize);
                var thumbUri = this.receiptStorage.AddReceipt(Path.Combine("thumbnails", imageName), thumb, "image/jpeg");

                var photo = ResizeImage(originalPhoto, PhotoSize);
                var photoUri = this.receiptStorage.AddReceipt(imageName, photo, "image/jpeg");

                this.expenseRepository.UpdateExpenseItemImages(message.Username, message.ExpenseId, expenseItemId, photoUri, thumbUri);

                this.receiptStorage.DeleteReceipt(expenseItemId);
            }
        }

        private static ImageCodecInfo GetImageCodec()
        {
            return ImageCodecInfo.GetImageEncoders()
                .Where(c => c.FormatID == ImageFormat.Jpeg.Guid)
                .Single();
        }

        private static EncoderParameters GetImageCodecParameters()
        {
            var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(Encoder.Quality, 80L);
            return parameters;
        }

        private static byte[] ResizeImage(byte[] imageData, int size)
        {
            var image = Image.FromStream(new MemoryStream(imageData));
            int newHeight;
            int newWidth;
            if (image.Width > image.Height)
            {
                newHeight = (int)Math.Round(image.Height*((1/(decimal)image.Width)*size));
                newWidth = size;
            }
            else
            {
                newWidth = (int)Math.Round(image.Width*((1/(decimal)image.Height)*size));
                newHeight = size;
            }

            var thumb = new Bitmap(size, size);
            using (var g = Graphics.FromImage(thumb))
            {
                int startX = (size - newWidth)/2;
                int startY = (size - newHeight)/2;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.White);
                g.DrawImage(image, startX, startY, newWidth, newHeight);
            }

            byte[] newImage;
            using (var stream = new MemoryStream())
            {
                thumb.Save(stream, GetImageCodec(), GetImageCodecParameters());
                newImage = stream.ToArray();
                stream.Close();
            }

            return newImage;
        }
    }
}