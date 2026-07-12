using CommonTestUtils;

[assembly: Epilogue]
#if WINDOWS
[assembly: ConfigureJoinableTaskFactory]
#endif
[assembly: TestAppSettings]
[assembly: Category("UnitTests")]
