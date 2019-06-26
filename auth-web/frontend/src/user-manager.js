import { createUserManager } from "redux-oidc";

const userManagerConfig = Object.assign({
  authority: `${window.location.protocol}//${window.location.hostname}${window.location.port ? `:${window.location.port}` : "" }/`,
  client_id: `sartracks-auth-ui`,
  redirect_uri: `${window.location.protocol}//${window.location.hostname}${window.location.port ? `:${window.location.port}` : "" }/loggedIn`,
  post_logout_redirect_uri: `${window.location.protocol}//${window.location.hostname}${window.location.port ? `:${window.location.port}` : ""}`,
  automaticSilentRenew: true,
  silent_redirect_uri: `${window.location.protocol}//${window.location.hostname}${window.location.port ? `:${window.location.port}` : ""}/silent-renew.html`,
  filterProtocolClaims: true,
  loadUserInfo: true,
  scope: "openid profile email",
  response_type: "token id_token",
});

const userManager = createUserManager(userManagerConfig);

export default userManager;