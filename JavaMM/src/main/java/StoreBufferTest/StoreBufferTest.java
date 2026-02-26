package StoreBufferTest;

import java.util.concurrent.CyclicBarrier;
import java.util.concurrent.BrokenBarrierException;

public class StoreBufferTest {
    // Java volatile: Guarantees a StoreLoad barrier, making the
    // r1=0 AND r2=0 outcome illegal / impossible under the JMM's rules.

    static volatile int x;
    static volatile int y;

    static int r1, r2;

    // Use a CyclicBarrier to align threads precisely, equivalent to C#'s Barrier
    // The number of parties is 2 (Thread1 and Thread2)
    static CyclicBarrier barrier;


    static void Thread1() throws InterruptedException, BrokenBarrierException {
        // Wait for both threads to be ready
        barrier.await();

        // Volatile Store
        x = 1;
        // Volatile Read (In Java, this read is guaranteed to see x=1 if it happens
        // after Thread2's x=1, but the reordering demonstrated in C# is prevented)
        int temp = y;

        r1 = temp;
    }

    static void Thread2() throws InterruptedException, BrokenBarrierException {
        // Wait for both threads to be ready
        barrier.await();

        // Volatile Store
        y = 1;
        // Volatile Read
        int temp = x;

        r2 = temp;
    }

    public static void main(String[] args) {
        System.out.println("Starting Store Buffer Reordering Test...");
        System.out.println("Goal: Prove C# volatile allows Store-Load reordering.");
        System.out.println("Target: r1=0 AND r2=0 (Impossible in Java/Seq. Consistency)");
        System.out.println("---------------------------------------------------------");

        long iteration = 0;
        while (true) {
            barrier = new CyclicBarrier(2);
            iteration++;
            x = 0; y = 0;
            r1 = -1; r2 = -1;

            // Runnable wrapper for Thread1 and Thread2 logic (Task.Factory) equivalent
            Runnable runnable1 = () -> {
                try {
                    Thread1();
                } catch (InterruptedException | BrokenBarrierException e) {
                    Thread.currentThread().interrupt();
                }
            };

            Runnable runnable2 = () -> {
                try {
                    Thread2();
                } catch (InterruptedException | BrokenBarrierException e) {
                    Thread.currentThread().interrupt();
                }
            };

            Thread t1 = new Thread(runnable1);
            Thread t2 = new Thread(runnable2);
            t1.start();
            t2.start();

            // Java equivalent to Task.WaitAll()
            try {
                t1.join();
                t2.join();
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                System.err.println("Main thread interrupted.");
                break;
            }

            // The desired result of r1=0 and r2=0 is *not* expected in Java
            // because of its stronger volatile memory guarantee.
            if (r1 == 0 && r2 == 0) {
                System.out.println("\n! REORDERING DETECTED on Iteration " + iteration + " !");
                System.out.println("r1 = " + r1 + ", r2 = " + r2);
                System.out.println("Conclusion: Unexpected reordering observed (Java is supposed to prevent this).");
                // This would be a finding AGAINST the standard JMM
                System.exit(1);
            }

            if (iteration % 100000 == 0) {
                System.out.println("Iteration: " + iteration + " - No reordering yet...");
            }
        }
    }
}