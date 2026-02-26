package volatileSemantics;

import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

public class ReleaseSemanticsTest {

    private int _data = 0;
    private volatile boolean _volatileFlag;

    private int observedData;
    private boolean sawFlagTrue;

    @Test
    public void Test_VolatileWrite_Prevents_Previous_Store_From_Hoisting_Down() throws InterruptedException {
        int N = 100_000;
        for (int i = 0; i < N; i++) {
            _data = 0;
            _volatileFlag = false;

            // Thread A: The Writer
            Thread t1 = new Thread(() ->
            {
                _data = 42;
                _volatileFlag = true; // Store (Release)
            });

            observedData = -1;
            sawFlagTrue = false;

            // Thread B: The Reader (Acquire)
            Thread t2 = new Thread(() ->
            {
                if (_volatileFlag) {
                    sawFlagTrue = true;
                    observedData = _data;
                }
            });

            t1.start();
            t2.start();
            t1.join();
            t2.join();

            if (sawFlagTrue) {
                Assertions.assertTrue(observedData == 42,
                        "Release failure at iteration " + i + ": Saw flag=true but data=0.");
            }
        }
    }
}
