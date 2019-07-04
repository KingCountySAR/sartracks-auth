import React, { Component } from 'react'

export default class FacebookPage extends Component {
  componentDidMount() {
    const script = document.createElement("script");

    script.src = "https://connect.facebook.net/en_US/sdk.js#xfbml=1&version=v3.3&appId=1741863986025959";
    script.async = true;
    script.crossOrigin='anonymous';

    this.facebookPluginDiv.dataset.height = Math.max(this.facebookPluginDiv.parentNode.clientHeight - 32, 400);
    document.body.appendChild(script);
  }

  render() {
    return (<div className="fb-page" ref={el => this.facebookPluginDiv = el} data-href="https://www.facebook.com/kingcountysar/" data-tabs="timeline" data-width="500" data-height="" data-small-header="false" data-adapt-container-width="true" data-hide-cover="false" data-show-facepile="true"><blockquote cite="https://www.facebook.com/kingcountysar/" className="fb-xfbml-parse-ignore"><a href="https://www.facebook.com/kingcountysar/">King County Search and Rescue</a></blockquote></div>)
  }
}