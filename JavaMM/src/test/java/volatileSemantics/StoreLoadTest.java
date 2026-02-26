package volatileSemantics;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.concurrent.BrokenBarrierException;
import java.util.concurrent.CyclicBarrier;

// Java volatile: Guarantees a StoreLoad barrier, making the
// r1=0 AND r2=0 outcome illegal / impossible under the JMM's rules.
public class StoreLoadTest {

    static volatile int x;
    static volatile int y;

    static int r1, r2;

    static CyclicBarrier barrier;

    static void Thread1() throws InterruptedException, BrokenBarrierException {
        barrier.await();

        x = 1; // Volatile Store

        int temp = y; // Volatile Load

        r1 = temp; // Store
    }

    static void Thread2() throws InterruptedException, BrokenBarrierException {
        barrier.await();

        y = 1;

        int temp = x;

        r2 = temp;
    }

    @Test
    public void Test_Store_Load_Reordering() {
        System.out.println("Starting Store Buffer Reordering Test...");

        int N = 100_000;
        for (int i = 0; i < N; i++) {
            barrier = new CyclicBarrier(2);
            x = 0;
            y = 0;
            r1 = -1;
            r2 = -1;

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

            try {
                t1.join();
                t2.join();
            } catch (InterruptedException e) {
                Thread.currentThread().interrupt();
                break;
            }

            // The desired result of r1=0 and r2=0 is *not* expected in Java
            // because of its stronger volatile memory guarantee.
            if (r1 == 0 && r2 == 0) {
                System.out.println("r1 = " + r1 + ", r2 = " + r2);
                // This would be a finding AGAINST the standard JMM
                Assertions.fail("REORDERING DETECTED on Iteration: " + i + " out of " + N +
                        "\n" + " Result: r1 = " + r1 + ", r2 = " + r2);
            }
        }
        System.out.println("Test Complete: No Load Store reordering detected");
    }
}
