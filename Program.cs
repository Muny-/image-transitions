using System;
using System.Drawing;
using System.Drawing.Imaging;
using ImageMagick;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace image_transitions
{
    class Program
    {
        static int NumThreads = Environment.ProcessorCount;

        static int num_frames = 60;
        static int animation_delay = 2;

        static void Main(string[] args)
        {
            Console.WriteLine($"# of Threads: {NumThreads}");

            DateTime startTime = DateTime.Now;

            MagickImage original = new MagickImage("sample_images/white.png");

            MagickImage target = new MagickImage("sample_images/target.png");

            List<MagickImage> originalTiles = original.CropToTiles((original.Width / NumThreads)+1, original.Height).Cast<MagickImage>().ToList();
            List<MagickImage> targetTiles = target.CropToTiles((original.Width / NumThreads)+1, original.Height).Cast<MagickImage>().ToList();

            List<MagickImage>[] outputTilesProcessed = new List<MagickImage>[NumThreads];

            //MagickImage working = (MagickImage)original.Clone(original.Width, original.Height);

            Console.WriteLine($"ogT cnt: {originalTiles.Count}, tt cnt: {targetTiles.Count}");

            List<Thread> threads = new List<Thread>();

            DateTime threadStartTime = DateTime.Now;

            for (int i = 0; i < NumThreads; i++)
            {
                int i_copied = i;
                Thread t = new Thread(delegate ()
                {
                    outputTilesProcessed[i_copied] = ProcessTile(originalTiles[i_copied], targetTiles[i_copied]);
                });

                t.Start();

                threads.Add(t);
            }

            while(!threads.All(t => !t.IsAlive))
            {
                Thread.Sleep(1);
            }

            Console.WriteLine($"Tile process time: {Math.Round((DateTime.Now - threadStartTime).TotalSeconds, 2)} seconds");

            DateTime stitchStartTime = DateTime.Now;

            using (MagickImageCollection gifCollection = new MagickImageCollection())
            {
                for (int i = 0; i < num_frames; i++)
                {
                    using(MagickImageCollection frameCollection = new MagickImageCollection())
                    {
                        for (int j = 0; j < NumThreads; j++)
                        {
                            frameCollection.Add(outputTilesProcessed[j][i]);
                        }

                        gifCollection.Add(frameCollection.AppendHorizontally());

                        gifCollection[i].AnimationDelay = animation_delay;
                    }
                }

                Console.WriteLine($"Stitch time: {Math.Round((DateTime.Now - stitchStartTime).TotalSeconds, 2)} seconds");

                DateTime optimizeWriteStartTime = DateTime.Now;

                gifCollection.Optimize();
                gifCollection.Write("output/output.gif");
                
                Console.WriteLine($"Optimize and write time: {Math.Round((DateTime.Now - optimizeWriteStartTime).TotalSeconds, 2)} seconds");
            }

            Console.WriteLine($"Done, total elapsed time: {Math.Round((DateTime.Now - startTime).TotalSeconds, 2)} seconds");

            //Gif animation = new Gif("output/output.gif", num_frames);

                /*using (MagickImageCollection collection = new MagickImageCollection())
                {
                    collection.Add(working);
                    collection[0].AnimationDelay = animation_delay;

                    //collection[0].Write("output/0.png");

                    for (int i = 1; i < num_frames; i++)
                    {
                        DateTime startFrameTime = DateTime.Now;
                        collection.Add(Method2_Magick((MagickImage)collection[i-1], target));

                        collection[i].AnimationDelay = animation_delay;

                        DateTime endFrameTime = DateTime.Now;

                        Console.WriteLine($"Frame {i}/{num_frames-1} took {Math.Round((endFrameTime - startFrameTime).TotalSeconds, 2)} seconds to render");

                        //Console.WriteLine($"{i}/{num_frames-1}");


                        //collection[i].Write($"output/{i}.png");

                        //working.Save($"output/{(char)i}.png");
                    }

                    DateTime startOptimizeAndSave = DateTime.Now;

                    collection.Optimize();

                    collection.Write("output/output.gif");

                    DateTime endOptimizeAndSave = DateTime.Now;

                    Console.WriteLine($"Optimize + save took {Math.Round((endOptimizeAndSave-startOptimizeAndSave).TotalSeconds, 2)} seconds");
                }

                DateTime endTime = DateTime.Now;

                Console.WriteLine($"Done, total elapsed time: {Math.Round((endTime-startTime).TotalSeconds, 2)} seconds");*/
        }

        static List<MagickImage> ProcessTile(MagickImage originalTile, MagickImage targetTile)
        {
            List<MagickImage> processedTiles = new List<MagickImage>();

            processedTiles.Add(originalTile);

            for (int i = 1; i < num_frames; i++)
            {
                Console.WriteLine($"{i}/{num_frames-1}");
                processedTiles.Add(Method2_Magick((MagickImage)processedTiles[i - 1], targetTile));
            }

            return processedTiles;
        }

        static MagickImage Method2_Magick(MagickImage working, MagickImage target)
        {
            MagickImage newImage = (MagickImage)working.Clone(/*working.Width, working.Height*/);

            int max_num_matches_per_frame = (int)(target.Height * target.Width * (1 / 1.0));

            //Console.WriteLine($"max_matches: {max_num_matches_per_frame}");

            IPixelCollection newImage_pixels = newImage.GetPixels();
            IPixelCollection target_pixels = target.GetPixels();

            /*for (int x = 0; x < newImage.Width; x++)
            {
                for (int y = 0; y < newImage.Height; y++)
                {
                    Pixel p = newImage_pixels.GetPixel(x, y);
                    Console.WriteLine($"p: {p.GetChannel(0)}, {p.GetChannel(1)}, {p.GetChannel(2)}");
                }
            }*/

            for (int i = 0; i < max_num_matches_per_frame; i++)
            {
                Random rand = new Random();

                int x = rand.Next(0, newImage.Width);
                int y = rand.Next(0, newImage.Height);

                Pixel p_targ = target_pixels.GetPixel(x, y);
                Pixel p_work = newImage_pixels.GetPixel(x, y);

                

                //Console.WriteLine($"x, y: {x}, {y}");
                //Console.WriteLine($"r,g,b: {p_work.ToColor().R}, {p_work.ToColor().G}, {p_work.ToColor().B}");

                int randR = rand.Next(0, 255);
                int randG = rand.Next(0, 255);
                int randB = rand.Next(0, 255);

                int newR = p_work.GetChannel(0);
                int newG = p_work.GetChannel(1);
                int newB = p_work.GetChannel(2);

                if (Math.Abs(randR - p_targ.GetChannel(0)) < Math.Abs(randR - p_work.GetChannel(0)))
                    newR = randR;

                if (Math.Abs(randG - p_targ.GetChannel(1)) < Math.Abs(randG - p_work.GetChannel(1)))
                    newG = randG;

                if (Math.Abs(randB - p_targ.GetChannel(2)) < Math.Abs(randB - p_work.GetChannel(2)))
                    newB = randB;

                //if (newR != c_work.R || newG != c_work.G || newB != c_work.B)
                //    num_matches++;

                p_work.SetChannel(0, (byte)newR);
                p_work.SetChannel(1, (byte)newG);
                p_work.SetChannel(2, (byte)newB);

                /*p_work.SetChannel(0, 255);
                p_work.SetChannel(1, 255);
                p_work.SetChannel(2, 255);*/

                //Console.WriteLine($"orig: {newImage_pixels.GetPixel(x, y).GetChannel(0)}, new: {p_work.GetChannel(0)}");

                newImage_pixels.SetPixel(p_work);
                //newImage_pixels.SetArea(x, y, 1, 1, new[] { newR, newG, newB });



                //working.SetPixel(x, y, Color.FromArgb(newR, newG, newB));
            }

            return newImage;
        }

        /*static void Method2(Bitmap working, Bitmap target)
        {
            int max_num_matches_per_frame = (int)(target.Height * target.Width * (1 / 5.0));

            for (int i = 0; i < max_num_matches_per_frame; i++)
            {
                Random rand = new Random();

                int x = rand.Next(0, working.Width);
                int y = rand.Next(0, working.Height);

                Color c_targ = target.GetPixel(x, y);
                Color c_work = working.GetPixel(x, y);

                int randR = rand.Next(0, 255);
                int randG = rand.Next(0, 255);
                int randB = rand.Next(0, 255);

                int newR = c_work.R;
                int newG = c_work.G;
                int newB = c_work.B;

                if (Math.Abs(randR - c_targ.R) < Math.Abs(randR - c_work.R))
                    newR = randR;

                if (Math.Abs(randG - c_targ.G) < Math.Abs(randG - c_work.G))
                    newG = randG;

                if (Math.Abs(randB - c_targ.B) < Math.Abs(randB - c_work.B))
                    newB = randB;

                //if (newR != c_work.R || newG != c_work.G || newB != c_work.B)
                //    num_matches++;

                working.SetPixel(x, y, Color.FromArgb(newR, newG, newB));
            }
        }*/

        static void Method1(Bitmap working, Bitmap target)
        {
            //int max_num_matches_per_frame = (int)(target.Height * target.Width * (1 / 25.0));

            //int num_matches = 0;

            for (int x = 0; x < target.Width /*&& num_matches < max_num_matches_per_frame*/; x++)
            {
                for (int y = 0; y < target.Height /*&& num_matches < max_num_matches_per_frame*/; y++)
                {
                    Color c_targ = target.GetPixel(x, y);
                    Color c_work = working.GetPixel(x, y);

                    Random rand = new Random();

                    int randR = rand.Next(0, 255);
                    int randG = rand.Next(0, 255);
                    int randB = rand.Next(0, 255);

                    int newR = c_work.R;
                    int newG = c_work.G;
                    int newB = c_work.B;

                    if (Math.Abs(randR - c_targ.R) < Math.Abs(randR - c_work.R))
                        newR = randR;

                    if (Math.Abs(randG - c_targ.G) < Math.Abs(randG - c_work.G))
                        newG = randG;

                    if (Math.Abs(randB - c_targ.B) < Math.Abs(randB - c_work.B))
                        newB = randB;

                    //if (newR != c_work.R || newG != c_work.G || newB != c_work.B)
                    //    num_matches++;

                    working.SetPixel(x, y, Color.FromArgb(newR, newG, newB));
                }
            }
        }
    }
}
