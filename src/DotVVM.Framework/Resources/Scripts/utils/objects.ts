export function isPrimitive(viewModel: any) {
    return !(viewModel instanceof Object);
}

export const createArray =
    compileConstants.nomodules ?
        <T>(a: ArrayLike<T>) => {
            return ([] as T[]).slice.apply(a)
        } :
        Array.from;

export const hasOwnProperty = (obj: any, prop: string) => Object.prototype.hasOwnProperty.call(obj, prop);

export const symbolOrDollar : (name: string) => symbol =
    compileConstants.nomodules ? ((name: string) => window["Symbol"] ? Symbol(name) as symbol : "$" + name as any as symbol)
                               : (name: string) => Symbol(name)

export const keys =
    compileConstants.nomodules ?
    ((o: any) => typeof o == "object" && o != null ? Object.keys(o) : []) :
    Object.keys
