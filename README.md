DLNA Server as a plugin.

Designed for Jellyfin 10.8 (https://github.com/jellyfin/jellyfin/pull/4543)

- Faster implementation.
- UDP port restrictions.
- SSDP packet tracing.
- DLNA debug logging.
- Permits custom DLNA server name.
- Simplified dlna profile detection code.
- Dlna server profile detection now uses dlna playto profiles (if available) to aid in dlna playback.
- More relaxed SOAP response parsing.
- Per profile user account assignment. Different devices can use different user accounts.
- Hardened security. DLNA will now fail if a user account hasn't been specified.

**Settings**

**EnableDebugLog**

Enables dlna server message recording.

**DefaultUserId**

Defines the account to use.

**AliveMessageIntervalSeconds**

Sets the frequency at which SSDP alive notifications are transmitted.

**DlnaServerName**

Defines a custom name for the dlna server.

**DefaultIconWidth**

Sets the default icon width.

**DefaultIconHeight**

Sets the default icon height.
        
**EnableMsMediaReceiverRegistrar**

Enable/Disables the MSMediaReceiverRegistrar service is active.

**BindAddresses**

Defines the interface addresses which the DLNA server will bind to. If empty, all the interfaces defined in JF will be used.


**Contains fixes for the following Jellyfin bugs**

6002,
5981,
5815,
5454,
5131,
4919,
2226,
5319,
6020
