using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TurtleCopy
{
    class Program
    {
        // Create a progress monitor
        static int progress;

        // Define a structure to use as the Timer's state.
        struct ProcessData
        {
            public IEnumerable<byte[]> source;
            public FileStream destination;
        }

        // Log pretty messages, but revert to the startup color.
        static ConsoleColor startColor;

        static void Main(string[] args)
        {
            // 3 Arguments are required, any more or less and you are denied.
            if(args.Count() != 3)
            {
                Console.WriteLine("Turtle copy is a slow file copy simulator, it must have a combination of 3 parameters.");
                Console.WriteLine("Usage:   TurtleCopy.exe PathToSourceFile PathToDestination WriteTimeInSeconds");
                Console.WriteLine("PathToSourceFile:  The path, including the file name of the source file, include quotes if path contains spaces.");
                Console.WriteLine("PathToDestinationFile:  The path, including the file name of the destination file, include quotes if path contains spaces.");
                Console.WriteLine("WriteTimeInSeconds:  Time, as a number, in seconds to spend on the copy process.\n\n");
            }

            try
            {
                // Parse the arguments
                string source = args[0];
                string destination = args[1];
                string delay = args[2];

                // Conver the delay to an integer and validate the others.
                int nDelay = ValidateAndConvert(source, delay);            
                
                // Set that start color value.    
                startColor = Console.ForegroundColor;

                // Do the grunt work.
                PrepWork(source, destination, nDelay);

                // Don't friggin' close this window.
                Console.ReadKey();
            }
            catch(Exception ex)
            {
                // Show the error in red.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error");
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = startColor;
                Console.ReadKey();
            }
        }

        private static int ValidateAndConvert(string source, string delay)
        {
            // File must be there for the source
            // Delay must be a valid integer and greater than zero.

            int value;
            if (!System.IO.File.Exists(source))
            {
                throw new System.IO.FileNotFoundException("A file does not exist at PathToSourceFile.");
            }

            if (!Int32.TryParse(delay, out value))
            {
                throw new System.InvalidCastException("WriteTimeInSeconds is not a valid 32 bit integer.");
            }

            if(value <= 0)
            {
                throw new System.InvalidCastException("WriteTimeInSeconds must be a whole number greater than zero.");
            }

            return value;
        }

        private static void PrepWork(string source, string destination, int delay)
        {
            var content = File.ReadAllBytes(source);
            var length = content.Length;

            // Get the rate.
            var rate = length / (delay - 1);
            
            // Add one unless we had a 0 remainder
            var chunkCount = (length / rate) + ((length % (delay - 1) != 0)?  1 : 0);

            byte[][] chunks = new byte[chunkCount][];

            int cpos = 0;
            int pass = 0;
            while(cpos < length)
            {
                // Split the content into exactly {delay} bytes, which should be each of {rate size}.
                var peekSize = rate;
                if ((cpos + rate) > length)
                {
                    // Adjust rate on this pass.
                    peekSize = length - cpos;
                }

                byte[] chunk = new byte[peekSize];
                Array.Copy(content, cpos, chunk, 0, peekSize);
                chunks[pass] = chunk;
                cpos += peekSize;
                pass++;
            }
            
            // Check our work, make sure we didn't hose up the file
            var sum = 0;
            foreach (var c in chunks) { sum += c.Length; }

            if(sum != length)
            {
                // We hosed up the file, warn the user.
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(string.Format("Sum of bytes '{0}' does not equal length '{1}'", sum, length));
                Console.ForegroundColor = startColor;
            }

            // Make sure the progress counter is set to zero.
            progress = 0;

            // Create our destination file with a .filepart extension
            var tempName = destination + ".filepart";
            FileStream fs = File.Create(tempName);

            // Set our state object.
            ProcessData d = new ProcessData() { destination = fs, source = chunks};

            // Invoke the timer
            var t = new System.Threading.Timer(Timer_Tick, d, 1000, 1000);          

            // Update progress.
            while(progress < chunks.Length)
            {
                Console.Write("\r{0} of {1} ", progress, chunks.Length);
                System.Threading.Thread.Sleep(1000);
            }

            // One last time...
            Console.Write("\r{0} of {1} ", progress, chunks.Length);

            // Clean up.
            t.Dispose();
            fs.Close();

            // Rename the file.
            File.Move(tempName, destination);

            // Wrap it up.
            Console.WriteLine("Done!");
            Console.ReadKey();
        }   

        private static void Timer_Tick(object state)
        {
            // Prevent threading accidents.
            lock (state)
            {
                // Using state and our global progress value, write a chunk of the file.
                ProcessData s = (ProcessData)state;
                if (progress < s.source.Count())
                {
                    byte[] chunk = s.source.ElementAt(progress);
                    s.destination.Write(chunk, 0, chunk.Length);
                }

                progress++;
            }
        }
    }
}
