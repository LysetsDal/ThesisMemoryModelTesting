package FinalImmutability;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.concurrent.BrokenBarrierException;
import java.util.concurrent.CyclicBarrier;

public class FinalImmutabilityTest {

    private CyclicBarrier barrier;

    private static FinalImmutability finalImmutability;

    private int observedData;

    private void Thread1() {
        try {
            barrier.await();
        } catch (InterruptedException | BrokenBarrierException e) {
            e.printStackTrace();
        }
        finalImmutability = new FinalImmutability(1);
    }

    private void Thread2() {
        try {
            barrier.await();
        } catch (InterruptedException | BrokenBarrierException e) {
            e.printStackTrace();
        }

        while (finalImmutability == null) {
        }

        observedData = finalImmutability.finalData;
    }

    @Test
    public void Test_Readonly_Can_Be_Default_Value_After_Ctor_Finishes() throws InterruptedException {
        System.out.println("Starting Test_Readonly_Can_Be_Default_Value_After_Ctor_Finishes()");
        int N = 100_000;

        for (int i = 0; i < N; i++) {
            barrier = new CyclicBarrier(2);
            finalImmutability = null;
            observedData = -1;

            // Thread 1: Constructs the class with the readonly field
            Thread t1 = new Thread(this::Thread1);
            // Thread 2: Reads the field after object construction finishes
            Thread t2 = new Thread(this::Thread2);

            t1.start();
            t2.start();
            t1.join();
            t2.join();

            // If readonly semantics failed, observedData should be 0
            Assertions.assertEquals(1, observedData);

            if (i % 10000 == 0) {
                System.out.println("Iteration: " + i);
            }
        }
        System.out.println("Completed Test_Readonly_Can_Be_Default_Value_After_Ctor_Finishes()");
    }
}
