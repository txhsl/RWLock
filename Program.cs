using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NET_Class
{
    class RWLock
    {
        /*
            1. No reading while writing and no writing while reading.
            2. Can read while someone else reading.
            3. Cannot write while somone else writing.
            4. While writer is waiting new reader should wait him.
            5. Support multiple writers in the quene.
        */

        private Mutex rcLock = new Mutex(false, "rcLock");// reader count lock
        private Mutex wcLock = new Mutex(false, "wcLock");// writer count lock
        private Mutex rwLock = new Mutex(false, "rwLock");// r/w state lock
        private Mutex rlLock = new Mutex(false, "rlLock");// lock of reader lock
        private Mutex wlLock = new Mutex(false, "wlLock");// lock of wirter lock

        private bool rLock = false;// reader lock
        private bool wLock = false;// writer lock

        private bool isWriterWaiting = false;
        private int rCount = 0; // the count of readers
        private int wCount = 0;// the count of waitng writers

        public void readerLock()
        {
            //no writer waiting or writing
            while (isWriterWaiting || wLock) { }
            rcLock.WaitOne();
            rCount += 1;

            //re-in
            if (rCount == 1)
            {
                Console.WriteLine("Read locked");
                rlLock.WaitOne();
                rLock = true;
                rlLock.ReleaseMutex();
            }
            else
                Console.WriteLine("Not first reader");
            rcLock.ReleaseMutex();

        }
        public void readerRelease()
        {
            //quit reading
            rcLock.WaitOne();
            rCount -= 1;

            if (rCount == 0)
            {
                Console.WriteLine("Read released");
                rlLock.WaitOne();
                rLock = false;
                rlLock.ReleaseMutex();
            }
            else
                Console.WriteLine("Not final reader");
            rcLock.ReleaseMutex();
        }
        public void writerLock()
        {
            //writer first in the quene
            rwLock.WaitOne();
            isWriterWaiting = true;
            rwLock.ReleaseMutex();

            wcLock.WaitOne();
            wCount += 1;
            wcLock.ReleaseMutex();

            lock (wlLock)
            {
                while (rLock || wLock) { }// no writing and reading

                Console.WriteLine("Write locked");
                wLock = true;
            }

            //quit waiting
            wcLock.WaitOne();
            wCount -= 1;
            if (wCount == 0)
            {
                //set readable
                rwLock.WaitOne();
                isWriterWaiting = false;
                rwLock.ReleaseMutex();
            }
            wcLock.ReleaseMutex();
        }
        public void writerRelease()
        {
            Console.WriteLine("Write released");
            wlLock.WaitOne();
            wLock = false;
            wlLock.ReleaseMutex();

        }
    }

    class Reader
    {
        RWLock rwLock = null;

        private Reader() { }
        public Reader(RWLock rwLock)
        {
            this.rwLock = rwLock;
        }
        public void getLock()
        {
            rwLock.readerLock();
        }
        public void releaseLock()
        {
            rwLock.readerRelease();
        }

    }

    class Writer
    {
        RWLock rwLock = null;

        private Writer() { }
        public Writer(RWLock rwLock)
        {
            this.rwLock = rwLock;
        }
        public void getLock()
        {
            rwLock.writerLock();
        }
        public void releaseLock()
        {
            rwLock.writerRelease();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            RWLock rwLock = new RWLock();
            Reader readerA = new Reader(rwLock);
            Reader readerB = new Reader(rwLock);
            Writer writerA = new Writer(rwLock);
            Writer writerB = new Writer(rwLock);
            Reader readerC = new Reader(rwLock);

            Thread readA = new Thread(new ThreadStart(delegate ()
            {
                readerA.getLock();
                Console.WriteLine("ReaderA start reading.");

                Thread.Sleep(5000);

                Console.WriteLine("ReaderA finish reading.");
                readerA.releaseLock();
            }));
            readA.Start();// first reader

            Thread readB = new Thread(new ThreadStart(delegate ()
            {
                Thread.Sleep(100);

                readerB.getLock();
                Console.WriteLine("ReaderB start reading.");

                Thread.Sleep(5000);

                Console.WriteLine("ReaderB finish reading.");
                readerB.releaseLock();
            }));
            readB.Start();// second reader while readerA is reading

            Thread writeA = new Thread(new ThreadStart(delegate ()
            {
                Thread.Sleep(1000);
                writerA.getLock();
                Console.WriteLine("WriterA start writing.");

                Thread.Sleep(5000);

                Console.WriteLine("WriterA finish writing.");
                writerA.releaseLock();
            }));
            writeA.Start();// first writer while readerA and readerB are reading

            Thread writeB = new Thread(new ThreadStart(delegate ()
            {
                Thread.Sleep(1000);
                writerB.getLock();
                Console.WriteLine("WriterB start writing.");

                Thread.Sleep(5000);

                Console.WriteLine("WriterB finish writing.");
                writerB.releaseLock();
            }));
            writeB.Start();// second writer while readerA and readerB are reading and writerA is waiting 

            Thread readC = new Thread(new ThreadStart(delegate ()
            {
                Thread.Sleep(2000);

                readerC.getLock();
                Console.WriteLine("ReaderC start reading.");

                Thread.Sleep(5000);

                Console.WriteLine("ReaderC finish reading.");
                readerC.releaseLock();
            }));
            readC.Start();// third reader while readerA and readerB are reading and writerA and writerB are waiting

            Thread.Sleep(20000);
        }
    }
}
