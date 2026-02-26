package safePublication;

class UnsafeInitialization {
    private int x;
    private Object o;

    public UnsafeInitialization() {
        this.x = 42;
        this.o = new Object();
    }

    public int readX() {
        return this.x;
    }

    public Object readO() {
        return this.o;
    }
}
