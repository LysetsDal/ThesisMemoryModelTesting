package safePublication;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.util.Objects;
import java.util.concurrent.CyclicBarrier;

class UnsafeInitializationTest {

    private UnsafeInitialization u;
    private CyclicBarrier barrier;

    // This code illustrates an issue with safe publication. Although,
    // in most systems and hardware, the issue with safe publication
    // is not observable when running the program.
    @Test
    public void TestUnsafeInitialization() throws InterruptedException {
        int N = 1_00_000;
        for (int i = 0; i < N; i++) {

            Thread t1 = new Thread(() -> {
                u = new UnsafeInitialization();
            });

            Thread t2 = new Thread(() -> {
                if (!Objects.isNull(u) && u.readX() != 42) {
                    Assertions.fail("T2: x is not equal 42");
                }
            });

            Thread t3 = new Thread(() -> {
                if (!Objects.isNull(u) && Objects.isNull(u.readO())) {
                    Assertions.fail("T3: o is null");
                }
            });

            t1.start();
            t2.start();
            t3.start();

            t1.join();
            t2.join();
            t3.join();

            u = null;
        }
    }
}


