package volatileSemantics;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Assertions;

public class AcquireSemanticsTest {
    private int normalInt1 = 0;
    private int normalInt2 = 0;
    private boolean volatileFlag;
    // Had to be moved up to class field in Java...?
    private int observedData;
    private boolean failedAcquire;
    private boolean t1SawT2StoreEarly;
    private volatile boolean acquireRead;

    @Test
    public void Test_VolatileRead_Prevents_Subsequent_Load_From_Hoisting() {
        int N = 100_000;

        for (int i = 0; i < N; i++) {
            normalInt1 = 0;
            volatileFlag = false;

            // Thread A: The Writer
            Thread t1 = new Thread(() -> {
                normalInt1 = 42;
                volatileFlag = true; // Volatile Store (Release Semantics)
            });

            failedAcquire = false;
            observedData = 0;

            // Thread B: The Reader
            Thread t2 = new Thread(() -> {
                boolean acquireRead = volatileFlag; // Volatile Load (Acquire Semantics)
                observedData = normalInt1;

                if (acquireRead && observedData == 0) {
                    failedAcquire = true;
                }
            });

            t1.start();
            t2.start();
            try {
                t1.join();
                t2.join();
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }

            Assertions.assertFalse(failedAcquire, "Acquire fence failed on iteration: " + i + ". Read of normalInt1 in t2 moved above volatile read of volatileFlag.");
        }
    }

    // This tests fails on Java...
    @Test
    public void Test_VolatileRead_Prevents_Subsequent_Store_From_Hoisting() {
        int N = 100_000;

        for (int i = 0; i < N; i++) {
            // Arrange
            volatileFlag = false;
            normalInt2 = 0;
            t1SawT2StoreEarly = false;
            acquireRead = false;

            Thread t1 = new Thread(() -> {
                // Note: If t2 hoists the Subsequent Store up above the Load, t1 might see
                // normalInt2 == 42 while volatileFlag is still false.
                if (normalInt2 == 42 && !volatileFlag) {
                    t1SawT2StoreEarly = true;
                }

                volatileFlag = true; // Volatile Read (Release Semantics)
            });

            Thread t2 = new Thread(() -> {
                acquireRead = volatileFlag; // Volatile Load (Acquire Semantics)
                normalInt2 = 42;            // Subsequent Store (Intra-thread Semantics)
            });

            t1.start();
            t2.start();
            try {
                t1.join();
                t2.join();
            } catch (InterruptedException e) {
                throw new RuntimeException(e);
            }

            Assertions.assertFalse(t1SawT2StoreEarly, "The store in t2, was == 42 before the volatileFlag was still false.\n" +
                    "That means variable 't1SawT2StoreEarly' was ");

        }
    }
}
