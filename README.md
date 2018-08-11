# Q4SRM
Quality assessment for QQQ SRM

Q4SRM reads the instrument method from a Thermo .RAW file, finds all transitions that have "heavy" or "hvy" in the "Name" (TSQ Vantage) or "Compound Name" (TSQ Altis) field of the method, then reads the data corresponding to those transitions and evaluates it against a set of metrics. The final output is a tab-separated value file, a scatter-plot showing summed intensity vs. time (and points color/shape-coded according to the metrics), and a PDF containing the above plot and basic plots of each transition group for quick evaluation of why it passed/failed the metric(s).
