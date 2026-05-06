package FinalImmutability;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;
import java.util.concurrent.CountDownLatch;

public class FinalFieldTest {

    public int count;

    @Test
    public void Test_FinalField_Example() throws InterruptedException {
        count = 0;
        for (int i = 0; i < 100000; i++) {
            runConcurrentTest();
        }
    }

    private void runConcurrentTest() throws InterruptedException {
        final FinalFieldClass ffc = new FinalFieldClass();
        CountDownLatch startLatch = new CountDownLatch(1);
        CountDownLatch finishLatch = new CountDownLatch(2);

        // Thread 1: The Writer
        Thread writerThread = new Thread(() -> {
            try {
                startLatch.await();
                ffc.writer();
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
            } finally {
                finishLatch.countDown();
            }
        });

        // Thread 2: The Reader
        Thread readerThread = new Thread(() -> {
            try {
                startLatch.await();
                // Busy wait until ffc is visible
                while (FinalFieldClass.instance == null) {
                    Thread.onSpinWait();
                }

                int xVal = ffc.x;
                int yVal = ffc.y;

                // x is final: JMM guarantees it must be 3
                Assertions.assertEquals(3, xVal, "Final field x must always be 3");

                // y is non-final: It COULD be 0, though it's rare on x86 architectures
                if (yVal == 0) {
                    count++;
                    System.out.println("Caught a partial initialization! y is 0.");
                }
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
            } finally {
                finishLatch.countDown();
            }
        });

        writerThread.start();
        readerThread.start();

        startLatch.countDown(); // Fire both threads
        finishLatch.await();    // Wait for completion
        FinalFieldClass.instance = null;
    }


    public class FinalFieldClass {
        final int x;
        int y;
        static FinalFieldClass instance;

        public FinalFieldClass() {
            x = 3;
            y = 4;
        }

        void writer() {
            instance = new FinalFieldClass();
        }

        static void reader() {
            if (instance != null) {
                int i = instance.x;  // guaranteed to see 3
                int j = instance.y;  // could see 0
                System.out.println("instance x: " + i);
                System.out.println("instance y: " + j);
            }
        }
    }
}
