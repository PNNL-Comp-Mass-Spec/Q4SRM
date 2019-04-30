# Q4SRM
Quality assessment for QQQ SRM

Q4SRM reads the instrument method from a Thermo .RAW file, finds all transitions that have "heavy" or "hvy" in the "Name" (TSQ Vantage) or "Compound Name" (TSQ Altis) field of the method, then reads the data corresponding to those transitions and evaluates it against a set of metrics. The final output is a tab-separated value file, a scatter-plot showing summed intensity vs. time for each heavy-labeled compound (and points color/shape-coded according to the metrics they failed), and a PDF containing the above plot and basic plots of each transition group for quick evaluation of why it passed/failed the metric(s).

MzML files are also supported, with an additional tab-/comma-delimited method file. Column headers are required in this method file, with the following headers: 'Compound Name', 'Precursor (m/z)', 'Product (m/z)', 'Start Time (min)', and 'End Time (min)'. 'Compound Name' column data is used to determine heavy transitions, as described above.

## Downloads
[GitHub Releases](https://github.com/PNNL-Comp-Mass-Spec/Q4SRM/releases)

## Wiki & Tutorial
* [Wiki](https://github.com/PNNL-Comp-Mass-Spec/Q4SRM/wiki)
* [Tutorial](https://github.com/PNNL-Comp-Mass-Spec/Q4SRM/wiki/Tutorial)

## Minimum requirements
Q4SRM is compiled against .NET 4.6.2, and uses Thermo's RawFileReader library. These lead to the following system requirements:
* 64-bit OS (for Thermo .RAW with RawFileReader); 32-bit OS is supported with mzML files, and is also supported for Thermo .RAW when 32-bit MSFileReader is installed.
* Windows 7 SP1 or newer with .NET 4.6.2 or newer installed.
* It is recommended that you have at least a dual-core CPU, and at least 4GB of memory.
