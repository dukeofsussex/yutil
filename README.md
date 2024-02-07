# YUtil (WIP)

[![GitHub license](https://img.shields.io/github/license/dukeofsussex/yutil)](https://github.com/dukeofsussex/rpuk-loganalyser/blob/master/LICENSE)

Small assortment of utilities for working with FiveM/GTA V files for myself, no support provided!

### Prerequisites

* [.NET](https://dotnet.microsoft.com)

## Build

1. Clone the repo
2. Build the C# project

## Usage

All directories are traversed recursively!

### Calculate YMAPs

Quickly calculate extents and flags for multiple YMAPs at once.

```
yUtil.exe ymaps calc <path/to/directory/with/ymaps>
```

### Intersect YMAPs

Combine YMAPs with identical names into one to fix resource conflicts.

```
yUtil.exe ymaps intersect <path/to/directory/with/ymaps>
```

### Analyse Yx files

Analyse your FiveM stream folder for a variety of common issues.

```
yUtil.exe analyse <path/to/directory/with/y-files>
```

## Contributing

Any contributions made are welcome and greatly appreciated.

1. Fork the project
2. Create your feature branch (`git checkout -b feature`)
3. Code it
4. Commit your changes (`git commit -m 'Add something awesome'`)
5. Push to the branch (`git push origin feature`)
6. Open a Pull Request

## License

This project is licensed under the GNU GPL License. See the [LICENSE](LICENSE) file for details.
