package safePublication;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Assertions;

import java.util.concurrent.CyclicBarrier;

public class LeakyConstructorTest {

    private CyclicBarrier barrier;

    public static volatile LeakyObject globalInstance;

    private int observedData = -1;

    @Test
    public void testFinalFieldPublication() throws InterruptedException {
        // High iteration count because race conditions are elusive
        int count = 0;
        int N = 100_000;

        for (int i = 0; i < N; i++) {
            barrier = new CyclicBarrier(2);
            globalInstance = null;
            observedData = -1;

            // Thread 1: Constructs the object
            Thread t1 = new Thread(() -> {
                try {
                    barrier.await();

                    new LeakyObject(42);

                } catch (Exception e) {
                    Thread.currentThread().interrupt();
                }
            });

            // Thread 2: Tries to read as the reference is visible
            Thread t2 = new Thread(() -> {
                try {
                    barrier.await();

                    while (globalInstance == null) {
                    }

                    observedData = globalInstance.data;

                } catch (Exception e) {
                    Thread.currentThread().interrupt();
                }
            });

            t1.start();
            t2.start();
            t1.join();
            t2.join();

            if (observedData == 0) {
                //  Assertions.fail("Final field was observed as 0 (default) at iteration " + i);
                count++;
            }
        }
        if (count > 0) {
            Assertions.fail("FAILURE: Readonly field was 0 " + count + " out of " + N + " iterations");
        }
    }

    public static class LeakyObject {
        public final int data;

        public LeakyObject(int value) {
            // "Constructor Escape": the reference is published
            // to globalInstance BEFORE 'data' is assigned.
            LeakyConstructorTest.globalInstance = this;

            // Assignment happens after publication
            this.data = value;
        }
    }
}