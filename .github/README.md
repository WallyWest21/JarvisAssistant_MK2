# GitHub Actions CI/CD Pipeline for Jarvis Assistant MAUI

This repository contains a comprehensive CI/CD pipeline for building and deploying the Jarvis Assistant .NET MAUI application across multiple platforms.

## 🚀 Workflows Overview

### 1. Main CI/CD Pipeline (`maui-ci.yml`)
**Triggers**: Push to main/develop/Voice-Fix, Pull Requests, Manual dispatch

**Features**:
- ✅ Cross-platform builds (Android, Windows, Android TV)
- 🧪 Automated testing with coverage reports
- 🔒 Security analysis with CodeQL
- 📦 Artifact generation for all platforms
- 🚨 Performance testing (on main/develop)
- 🎯 Automated releases from main branch
- 📢 Build notifications

### 2. Android Release Builder (`android-release.yml`)
**Triggers**: Manual dispatch only

**Features**:
- 📱 Signed APK for Android phones
- 📺 Optimized APK for Android TV
- 🏪 AAB (Android App Bundle) for Google Play Store
- 🔐 Keystore-based signing
- 📋 Automated version management
- 📦 GitHub release creation

### 3. Windows Release Builder (`windows-release.yml`)
**Triggers**: Manual dispatch only

**Features**:
- 🏪 MSIX package for Microsoft Store
- 💻 Sideload MSIX for manual installation
- 🎯 Portable self-contained executable
- 📀 Traditional Windows installer (Inno Setup)
- 📋 Multi-format packaging
- 📦 GitHub release creation

### 4. Dependency & Security Monitor (`dependency-security.yml`)
**Triggers**: Weekly schedule (Mondays 9 AM UTC), Manual dispatch

**Features**:
- 🔄 Automated dependency updates (patch versions)
- 🛡️ Security vulnerability scanning
- 📊 Deprecated package detection
- 🤖 Automated Pull Request creation
- 🚨 Security issue creation
- 📈 .NET workload updates

## 📋 Prerequisites

### Repository Secrets (Required for Signed Builds)

For Android releases, set these secrets in your GitHub repository:

```
ANDROID_KEYSTORE_BASE64  # Base64 encoded keystore file
ANDROID_KEY_ALIAS        # Key alias from keystore
ANDROID_KEY_PASSWORD     # Key password
ANDROID_STORE_PASSWORD   # Keystore password
```

### Creating Android Keystore

```bash
# Generate a new keystore
keytool -genkey -v -keystore jarvis-release-key.keystore -alias jarvis-key -keyalg RSA -keysize 2048 -validity 10000

# Convert to base64 for GitHub secrets
base64 -i jarvis-release-key.keystore | pbcopy  # macOS
base64 -w 0 jarvis-release-key.keystore          # Linux
certutil -encode jarvis-release-key.keystore jarvis-release-key.txt  # Windows
```

## 🛠️ Build Configurations

### Platform Targets
- **Android**: `net8.0-android` (API 21+)
- **Windows**: `net8.0-windows10.0.19041.0`
- **Android TV**: Optimized Android build with TV-specific settings

### Build Variants

#### Android
- **Debug APK**: Development builds with debugging enabled
- **Release APK**: Optimized for distribution
- **Android TV APK**: TV-optimized with specific manifest settings
- **AAB (App Bundle)**: For Google Play Store distribution

#### Windows
- **Store MSIX**: Microsoft Store ready package
- **Sideload MSIX**: Manual installation package
- **Portable EXE**: Self-contained executable
- **Installer EXE**: Traditional Windows installer

## 🚀 Running Builds

### Automatic Builds
- **Every push** to main/develop/Voice-Fix branches
- **Every pull request** to main/develop
- **Weekly dependency updates** (Mondays)

### Manual Builds

#### Release Android APK
1. Go to Actions → "Build Signed Android Release"
2. Click "Run workflow"
3. Enter version number (e.g., "1.0.0")
4. Choose whether to create GitHub release
5. Click "Run workflow"

#### Release Windows Package
1. Go to Actions → "Build Windows Store Package"
2. Click "Run workflow"
3. Enter version number (e.g., "1.0.0")
4. Choose whether to create GitHub release
5. Click "Run workflow"

## 📁 Artifact Structure

### Android Artifacts
```
android-apk/
├── JarvisAssistant-{version}-phone.apk     # Phone APK
├── JarvisAssistant-{version}-tv.apk        # Android TV APK
└── JarvisAssistant-{version}.aab           # App Bundle
```

### Windows Artifacts
```
windows-release/
├── JarvisAssistant-{version}-store.msix      # Store package
├── JarvisAssistant-{version}-sideload.msix   # Sideload package
├── JarvisAssistant-{version}-portable.exe    # Portable executable
└── JarvisAssistant-{version}-setup.exe       # Installer
```

## 🔒 Security Features

### Code Analysis
- **CodeQL scanning** for security vulnerabilities
- **Dependency vulnerability scanning** 
- **Automated security issue creation**
- **Weekly security audits**

### Best Practices
- Secrets are never logged or exposed
- Keystores are cleaned up after builds
- All artifacts are scanned before release
- Security reports are generated weekly

## 🧪 Testing Strategy

### Automated Tests
- **Unit tests** run on every build
- **Integration tests** for critical components
- **Performance tests** on main/develop branches
- **Code coverage** reporting

### Test Artifacts
- Test results (TRX format)
- Coverage reports
- Performance benchmarks
- Failure diagnostics

## 📊 Monitoring & Notifications

### Build Status
- ✅ Success notifications with platform status
- ❌ Failure notifications with error details
- 📈 Performance tracking over time
- 📋 Artifact size monitoring

### Reports Generated
- **Build reports** for each platform
- **Test coverage reports**
- **Security audit reports**
- **Dependency update reports**
- **Performance benchmark reports**

## 🔧 Customization

### Adding New Platforms
1. Update `TargetFrameworks` in project file
2. Add platform-specific build job to `maui-ci.yml`
3. Configure platform-specific properties
4. Add artifact upload steps

### Modifying Build Parameters
Edit the `env` section in workflow files:
```yaml
env:
  DOTNET_VERSION: '8.0.x'      # .NET version
  ANDROID_API_LEVEL: '34'      # Android API level
  JAVA_VERSION: '11'           # Java version for Android
```

### Custom Build Steps
Add steps to the appropriate job in workflow files:
```yaml
- name: Custom Build Step
  run: |
    echo "Custom build logic here"
    # Add your custom commands
```

## 📚 Troubleshooting

### Common Issues

#### Android Build Failures
- **Keystore issues**: Verify secrets are set correctly
- **SDK missing**: Check Android SDK installation
- **Java version**: Ensure Java 11 is used

#### Windows Build Failures
- **MSIX signing**: Verify certificate configuration
- **Dependencies**: Check .NET MAUI workload installation
- **Platform version**: Ensure Windows 10 19041.0+ target

#### Test Failures
- **Missing dependencies**: Run `dotnet restore`
- **Platform specific**: Check target framework compatibility
- **Environment**: Verify CI environment setup

### Debug Steps
1. Check workflow logs in GitHub Actions
2. Download and examine artifact contents
3. Run builds locally to reproduce issues
4. Verify all secrets and configurations

## 🚀 Deployment

### Android
- **Direct APK**: Download and install APK files
- **Google Play**: Upload AAB through Play Console
- **Enterprise**: Use MDM solutions for distribution

### Windows
- **Microsoft Store**: Submit MSIX through Partner Center
- **Direct Install**: Use sideload MSIX with PowerShell
- **Enterprise**: Deploy via WSUS or Intune
- **Portable**: Direct executable distribution

## 📈 Future Enhancements

### Planned Features
- [ ] Automated testing on real devices
- [ ] Performance regression detection
- [ ] Automated store submission
- [ ] Multi-language support builds
- [ ] Feature flag deployment
- [ ] A/B testing integration

### Contributing
1. Fork the repository
2. Create feature branch
3. Add/modify workflow files
4. Test changes thoroughly
5. Submit pull request

---

**Need Help?** Create an issue in the repository or check the [GitHub Actions documentation](https://docs.github.com/en/actions).
