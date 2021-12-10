using System;
using System.IO;
using NetVips;
using NetVips.Extensions;
using QRCodeDecoderLibrary;

namespace imgCrop
{
    class Program
    {
        static void Main(string[] args)
        {

            // current directory
            string CurDir = Environment.CurrentDirectory;
            string WorkDir = CurDir.Replace("bin\\Debug", "Work");
            if(WorkDir != CurDir && Directory.Exists(WorkDir)) Environment.CurrentDirectory = WorkDir;
 
            //trace
            #if DEBUG
            // open trace file
            QRCodeTrace.Open("TraceQRCodeDecoder.txt");
            QRCodeTrace.Format("**** {0}", "Crop a QR-Code from a file");
            #endif

            Console.WriteLine("Crop a QR-Code from a file");
            if (ModuleInitializer.VipsInitialized)
            {
                Console.WriteLine($"Inited libvips {NetVips.NetVips.Version(0)}.{NetVips.NetVips.Version(1)}.{NetVips.NetVips.Version(2)}");
                //trace
                #if DEBUG
		        QRCodeTrace.Format("Inited libvips {0}.{1}.{2}", NetVips.NetVips.Version(0), NetVips.NetVips.Version(1), NetVips.NetVips.Version(2));
                #endif
            }
            else
            {
                Console.WriteLine(ModuleInitializer.Exception.Message);
                //trace
                #if DEBUG
		        QRCodeTrace.Format("{0}", ModuleInitializer.Exception.Message);
                #endif
            }

            var dpi = "1200";
            var path = "";
            var fileoptions = "";
            if (args.Length > 0) dpi = args[0];
            if (args.Length > 1) path = args[1];
            if (args.Length > 2) fileoptions = args[2];


            foreach (var file in Directory.GetFiles(path))
            {
                // create decoder
                var QRCodeDecoder = new QRDecoder();

                var filename = Path.GetFileName(file).ToLower();
                var fullname = file.ToLower();
                
                Console.WriteLine("Read QR Bill from ["+fullname+"] with parameters ["+fileoptions+"]");
                //trace
                #if DEBUG
                QRCodeTrace.Format("==============");
                QRCodeTrace.Format("Read QR Bill from [{0}] with parameters [{1}]", fullname, fileoptions);
                #endif

                // Read PDF and get Metha data
                var loader = "";
                var pages = 1;
                var image = Image.NewFromFile(fullname); // NewFromFile -> Pdfload

                // store file read to check 
                image.WriteToFile(Environment.CurrentDirectory+"/"+filename.Split(".")[0]+"_READ.png");

                // file metadata
                //trace
                #if DEBUG
                QRCodeTrace.Format("{0}", "Metadata");
                #endif
                foreach (var field in image.GetFields()) {
                    //trace
                    #if DEBUG
                    QRCodeTrace.Format("\t{0}: {1}", field, image.Get(field));
                    #endif
                    if (field.Equals("n-pages")) pages = Convert.ToInt32(image.Get(field));
                    if (field.Equals("vips-loader")) loader = Convert.ToString(image.Get(field));
                }
                
                //
                for (var page = 1; page <= pages; page++) {
                    // load single page, it's easyer to get the QR-Code
                    if (loader.Equals("pdfload")) {
                        image = Image.NewFromFile(fullname+"[dpi="+dpi+",page="+(page-1)+","+fileoptions+"]");
                    }

                    if (loader.Equals("pngload") || loader.Equals("jpegload")) {
                        image = Image.NewFromFile(fullname+"["+fileoptions+"]");

                        // Identify if it is A4 or A5/6
                        var size = 297; 
                        if (Math.Round(Convert.ToDecimal(image.Width)/Convert.ToDecimal(image.Height)) == 2) size = 105;

                        var x_ratio = Convert.ToDouble((image.Width/image.Xres)/210);
                        var x_resolution = image.Xres * x_ratio * 25.4;
                        var x_correction = Convert.ToInt32(dpi)/x_resolution;

                        var y_ratio = Convert.ToDouble((image.Height/image.Yres)/size);
                        var y_resolution = image.Yres * y_ratio * 25.4;
                        var y_correction = Convert.ToInt32(dpi)/y_resolution;
                        
                        image = image.Resize(scale: x_correction, vscale: y_correction);
                        image = image.Copy(xres: image.Xres * x_ratio * x_correction, yres: image.Yres * y_ratio * y_correction);
                    }
                    
                    // Page to process
                    Console.WriteLine("Process file: {0}, page: {1}", fullname, page);
                    //trace
                    #if DEBUG
                    QRCodeTrace.Format("Process file: {0}, page: {1}", fullname, page);
                    #endif

                    var pw_mm = Convert.ToInt32(Math.Round(image.Width/image.Xres));
                    var ph_mm = Convert.ToInt32(Math.Round(image.Height/image.Yres));

                    //trace
                    #if DEBUG
                    QRCodeTrace.Format("Page dimension width: {0}, height: {1} in pixel, and width: {2}, height: {3} in mm", image.Width, image.Height, pw_mm, ph_mm);
                    #endif


                    var test = 5;
                    while (test > 0) {
                        Image cropimage;
                        string qrresult = "";

                        var header = 7.0;
                        if (test == 4) header = header / 2;
                        if (test == 3) header = 0;

                        var x = Convert.ToInt32(( 5 + 52 + 5 + 2 ) * image.Xres);
                        var y = image.Height - Convert.ToInt32(( 105 - 5 - header - 5 ) * image.Yres);
                        var w = Convert.ToInt32((46 + 10) * image.Xres);
                        var h = Convert.ToInt32((46 + 10) * image.Yres);

                        Console.WriteLine("QR-Code position x: {0}, y: {1} and dimension width: {2}, height: {3} in pixel", x, y, w, h);
                        cropimage = image.Crop(x, y, w, h);
                        cropimage.WriteToFile(Environment.CurrentDirectory+"/"+filename.Split(".")[0]+"_P"+page+"_QRCODE.png");

                        // decode image
                        byte[][] DataByteArray = QRCodeDecoder.ImageDecoder(BitmapConverter.ToBitmap(cropimage));
                        if (DataByteArray != null)
                            qrresult = QRDecoder.ByteArrayToStr(DataByteArray[0]);

                        if (test == 2) {
                            Console.WriteLine("Search QR-Code by changing signatur presision");

                            var count = 1;
                            var min_precision = 0.25;
                            var max_precision = 0.5;
                            var step_prcision = 0.01;
                            for (var precision = min_precision; precision <= max_precision; precision += step_prcision) {
                                QRCodeDecoder.SetDeviation(precision);
    
                                Console.WriteLine("Test {0} SigPrecision:{1:0.00}", count, precision);
                                QRCodeTrace.Format("Test {0} SigPrecision:{1:0.00}", count, precision);

                                DataByteArray = QRCodeDecoder.ImageDecoder(BitmapConverter.ToBitmap(cropimage));
                                QRCodeDecoder.DisplayBlackAndWhiteImage(Environment.CurrentDirectory + "/" + filename.Split(".")[0] + "_P" + page + "_QRCODE_TEST_" + count + ".png");
                                if (DataByteArray != null) { 
                                    break;
                                }

                                count++;
                            }

                            if (DataByteArray != null)
                                qrresult = QRDecoder.ByteArrayToStr(DataByteArray[0]);
                        }

                        if (test == 1) {
                            Console.WriteLine("Search QR-Code on the hole page");
                            DataByteArray = QRCodeDecoder.ImageDecoder(BitmapConverter.ToBitmap(image));
                            QRCodeDecoder.DisplayBlackAndWhiteImage(Environment.CurrentDirectory + "/" + filename.Split(".")[0] + "_P" + page + "_QRCODE_TEST_HolePage.png");

                            if (DataByteArray != null)
                                qrresult = QRDecoder.ByteArrayToStr(DataByteArray[0]);
                        }

                        if (!qrresult.Equals("")) {
                            Console.WriteLine("QR-Code gefunden *");
                            QRCodeTrace.Format("QR-Code gefunden *");
                            //QRCodeTrace.Format(qrresult);
                            test = 0;
                        } else {
                            Console.WriteLine("Kein QR-Code gefunden");
                            QRCodeTrace.Format("Kein QR-Code gefunden");
                            test--;
                        }
                    }
                }
            }
        }
    }
}
